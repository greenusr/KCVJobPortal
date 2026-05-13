param(
    [string]$Server = "localhost",
    [string]$OldDatabase = "JobPortalDatabase",
    [string]$NewDatabase = "KCVJobPortalDatabase"
)

$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Data

function Quote-Name([string]$name) {
    "[" + $name.Replace("]", "]]") + "]"
}

function Quote-Literal([string]$value) {
    "'" + $value.Replace("'", "''") + "'"
}

function New-Connection([string]$database) {
    $cs = "Data Source=$Server;Initial Catalog=$database;Integrated Security=True;Connection Timeout=30"
    New-Object System.Data.SqlClient.SqlConnection($cs)
}

function Invoke-Scalar([string]$database, [string]$sql) {
    $conn = New-Connection $database
    try {
        $conn.Open()
        $cmd = $conn.CreateCommand()
        $cmd.CommandTimeout = 0
        $cmd.CommandText = $sql
        $cmd.ExecuteScalar()
    } finally {
        if ($conn.State -eq "Open") { $conn.Close() }
    }
}

function Invoke-NonQuery([string]$database, [string]$sql) {
    $conn = New-Connection $database
    try {
        $conn.Open()
        $cmd = $conn.CreateCommand()
        $cmd.CommandTimeout = 0
        $cmd.CommandText = $sql
        [void]$cmd.ExecuteNonQuery()
    } finally {
        if ($conn.State -eq "Open") { $conn.Close() }
    }
}

function Invoke-Table([string]$database, [string]$sql) {
    $conn = New-Connection $database
    try {
        $conn.Open()
        $cmd = $conn.CreateCommand()
        $cmd.CommandTimeout = 0
        $cmd.CommandText = $sql
        $adapter = New-Object System.Data.SqlClient.SqlDataAdapter($cmd)
        $table = New-Object System.Data.DataTable
        [void]$adapter.Fill($table)
        $table
    } finally {
        if ($conn.State -eq "Open") { $conn.Close() }
    }
}

function Format-DataType($row) {
    $type = [string]$row.type_name
    $maxLength = [int]$row.max_length
    $precision = [int]$row.precision
    $scale = [int]$row.scale

    switch ($type.ToLowerInvariant()) {
        "varchar" { return "varchar(" + ($(if ($maxLength -eq -1) { "max" } else { $maxLength })) + ")" }
        "char" { return "char($maxLength)" }
        "varbinary" { return "varbinary(" + ($(if ($maxLength -eq -1) { "max" } else { $maxLength })) + ")" }
        "binary" { return "binary($maxLength)" }
        "nvarchar" { return "nvarchar(" + ($(if ($maxLength -eq -1) { "max" } else { [int]($maxLength / 2) })) + ")" }
        "nchar" { return "nchar(" + [int]($maxLength / 2) + ")" }
        "decimal" { return "decimal($precision,$scale)" }
        "numeric" { return "numeric($precision,$scale)" }
        "datetime2" { return "datetime2($scale)" }
        "datetimeoffset" { return "datetimeoffset($scale)" }
        "time" { return "time($scale)" }
        default { return $type }
    }
}

$oldExists = Invoke-Scalar "master" "SELECT COUNT(*) FROM sys.databases WHERE name = $(Quote-Literal $OldDatabase)"
if ([int]$oldExists -ne 1) {
    throw "Old database '$OldDatabase' was not found. Stopping without changes."
}

$newExists = Invoke-Scalar "master" "SELECT COUNT(*) FROM sys.databases WHERE name = $(Quote-Literal $NewDatabase)"
if ([int]$newExists -ne 0) {
    throw "Target database '$NewDatabase' already exists. Stopping to avoid overwriting data."
}

$tables = Invoke-Table $OldDatabase @"
SELECT s.name AS schema_name, t.name AS table_name, t.object_id
FROM sys.tables t
JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE t.is_ms_shipped = 0
ORDER BY s.name, t.name
"@

$columns = Invoke-Table $OldDatabase @"
SELECT
    s.name AS schema_name,
    t.name AS table_name,
    c.name AS column_name,
    c.column_id,
    ty.name AS type_name,
    c.max_length,
    c.precision,
    c.scale,
    c.is_nullable,
    c.is_identity,
    c.is_computed,
    dc.name AS default_name,
    dc.definition AS default_definition
FROM sys.tables t
JOIN sys.schemas s ON s.schema_id = t.schema_id
JOIN sys.columns c ON c.object_id = t.object_id
JOIN sys.types ty ON ty.user_type_id = c.user_type_id
LEFT JOIN sys.default_constraints dc ON dc.parent_object_id = t.object_id AND dc.parent_column_id = c.column_id
WHERE t.is_ms_shipped = 0
ORDER BY s.name, t.name, c.column_id
"@

$pkColumns = Invoke-Table $OldDatabase @"
SELECT
    s.name AS schema_name,
    t.name AS table_name,
    kc.name AS constraint_name,
    c.name AS column_name,
    ic.key_ordinal,
    i.type_desc
FROM sys.key_constraints kc
JOIN sys.tables t ON t.object_id = kc.parent_object_id
JOIN sys.schemas s ON s.schema_id = t.schema_id
JOIN sys.indexes i ON i.object_id = kc.parent_object_id AND i.index_id = kc.unique_index_id
JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
WHERE kc.type = 'PK'
ORDER BY s.name, t.name, ic.key_ordinal
"@

$identityCandidates = @{}
foreach ($group in ($pkColumns | Group-Object schema_name, table_name)) {
    if ($group.Count -ne 1) { continue }
    $pk = $group.Group[0]
    $col = $columns | Where-Object {
        $_.schema_name -eq $pk.schema_name -and $_.table_name -eq $pk.table_name -and $_.column_name -eq $pk.column_name
    } | Select-Object -First 1

    if ($col -and -not [bool]$col.is_identity -and @("int", "bigint", "smallint") -contains ([string]$col.type_name).ToLowerInvariant()) {
        $identityCandidates["$($pk.schema_name).$($pk.table_name).$($pk.column_name)"] = $true
    }
}

Write-Output "Creating database $NewDatabase"
Invoke-NonQuery "master" "CREATE DATABASE $(Quote-Name $NewDatabase);"

foreach ($table in $tables) {
    $schemaName = [string]$table.schema_name
    if ($schemaName -ne "dbo") {
        $schemaExists = Invoke-Scalar $NewDatabase "SELECT COUNT(*) FROM sys.schemas WHERE name = $(Quote-Literal $schemaName)"
        if ([int]$schemaExists -eq 0) {
            Invoke-NonQuery $NewDatabase "CREATE SCHEMA $(Quote-Name $schemaName);"
        }
    }

    $tableColumns = $columns | Where-Object { $_.schema_name -eq $schemaName -and $_.table_name -eq $table.table_name -and -not [bool]$_.is_computed } | Sort-Object column_id
    $defs = @()
    foreach ($col in $tableColumns) {
        $key = "$schemaName.$($table.table_name).$($col.column_name)"
        $identity = ""
        if ([bool]$col.is_identity -or $identityCandidates.ContainsKey($key)) {
            $identity = " IDENTITY(1,1)"
        }
        $nullText = " NOT NULL"
        if ([bool]$col.is_nullable) {
            $nullText = " NULL"
        }
        $defaultText = ""
        if ($col.default_definition -ne [DBNull]::Value -and -not [string]::IsNullOrWhiteSpace([string]$col.default_definition)) {
            $defaultName = if ($col.default_name -ne [DBNull]::Value) { [string]$col.default_name } else { "DF_$($table.table_name)_$($col.column_name)" }
            $defaultText = " CONSTRAINT $(Quote-Name $defaultName) DEFAULT $($col.default_definition)"
        }
        $defs += "    $(Quote-Name $col.column_name) $(Format-DataType $col)$identity$nullText$defaultText"
    }

    $createSql = "CREATE TABLE $(Quote-Name $schemaName).$(Quote-Name $table.table_name) (`n" + ($defs -join ",`n") + "`n);"
    Invoke-NonQuery $NewDatabase $createSql
}

foreach ($group in ($pkColumns | Group-Object schema_name, table_name)) {
    $first = $group.Group[0]
    $cols = ($group.Group | Sort-Object key_ordinal | ForEach-Object { Quote-Name $_.column_name }) -join ", "
    $cluster = if ([string]$first.type_desc -eq "CLUSTERED") { "CLUSTERED" } else { "NONCLUSTERED" }
    Invoke-NonQuery $NewDatabase "ALTER TABLE $(Quote-Name $first.schema_name).$(Quote-Name $first.table_name) ADD CONSTRAINT $(Quote-Name $first.constraint_name) PRIMARY KEY $cluster ($cols);"
}

foreach ($table in $tables) {
    $schemaName = [string]$table.schema_name
    $tableName = [string]$table.table_name
    $tableColumns = $columns | Where-Object { $_.schema_name -eq $schemaName -and $_.table_name -eq $tableName -and -not [bool]$_.is_computed } | Sort-Object column_id
    $colList = ($tableColumns | ForEach-Object { Quote-Name $_.column_name }) -join ", "
    $hasIdentity = $false
    foreach ($col in $tableColumns) {
        $key = "$schemaName.$tableName.$($col.column_name)"
        if ([bool]$col.is_identity -or $identityCandidates.ContainsKey($key)) { $hasIdentity = $true; break }
    }
    $qualified = "$(Quote-Name $schemaName).$(Quote-Name $tableName)"
    $copySql = ""
    if ($hasIdentity) { $copySql += "SET IDENTITY_INSERT $qualified ON;`n" }
    $copySql += "INSERT INTO $qualified ($colList) SELECT $colList FROM $(Quote-Name $OldDatabase).$qualified;`n"
    if ($hasIdentity) { $copySql += "SET IDENTITY_INSERT $qualified OFF;`n" }
    Invoke-NonQuery $NewDatabase $copySql
}

$foreignKeys = Invoke-Table $OldDatabase @"
SELECT
    fk.name AS fk_name,
    sp.name AS parent_schema,
    tp.name AS parent_table,
    sr.name AS ref_schema,
    tr.name AS ref_table,
    fk.delete_referential_action_desc,
    fk.update_referential_action_desc,
    fkc.constraint_column_id,
    cp.name AS parent_column,
    cr.name AS ref_column
FROM sys.foreign_keys fk
JOIN sys.tables tp ON tp.object_id = fk.parent_object_id
JOIN sys.schemas sp ON sp.schema_id = tp.schema_id
JOIN sys.tables tr ON tr.object_id = fk.referenced_object_id
JOIN sys.schemas sr ON sr.schema_id = tr.schema_id
JOIN sys.foreign_key_columns fkc ON fkc.constraint_object_id = fk.object_id
JOIN sys.columns cp ON cp.object_id = fkc.parent_object_id AND cp.column_id = fkc.parent_column_id
JOIN sys.columns cr ON cr.object_id = fkc.referenced_object_id AND cr.column_id = fkc.referenced_column_id
ORDER BY fk.name, fkc.constraint_column_id
"@

foreach ($group in ($foreignKeys | Group-Object fk_name)) {
    $first = $group.Group[0]
    $parentCols = ($group.Group | Sort-Object constraint_column_id | ForEach-Object { Quote-Name $_.parent_column }) -join ", "
    $refCols = ($group.Group | Sort-Object constraint_column_id | ForEach-Object { Quote-Name $_.ref_column }) -join ", "
    $sql = "ALTER TABLE $(Quote-Name $first.parent_schema).$(Quote-Name $first.parent_table) WITH CHECK ADD CONSTRAINT $(Quote-Name $first.fk_name) FOREIGN KEY ($parentCols) REFERENCES $(Quote-Name $first.ref_schema).$(Quote-Name $first.ref_table) ($refCols)"
    if ($first.delete_referential_action_desc -ne "NO_ACTION") { $sql += " ON DELETE " + $first.delete_referential_action_desc.Replace("_", " ") }
    if ($first.update_referential_action_desc -ne "NO_ACTION") { $sql += " ON UPDATE " + $first.update_referential_action_desc.Replace("_", " ") }
    $sql += "; ALTER TABLE $(Quote-Name $first.parent_schema).$(Quote-Name $first.parent_table) CHECK CONSTRAINT $(Quote-Name $first.fk_name);"
    Invoke-NonQuery $NewDatabase $sql
}

$checkConstraints = Invoke-Table $OldDatabase @"
SELECT s.name AS schema_name, t.name AS table_name, cc.name AS constraint_name, cc.definition
FROM sys.check_constraints cc
JOIN sys.tables t ON t.object_id = cc.parent_object_id
JOIN sys.schemas s ON s.schema_id = t.schema_id
WHERE cc.is_ms_shipped = 0
ORDER BY s.name, t.name, cc.name
"@
foreach ($cc in $checkConstraints) {
    Invoke-NonQuery $NewDatabase "ALTER TABLE $(Quote-Name $cc.schema_name).$(Quote-Name $cc.table_name) WITH CHECK ADD CONSTRAINT $(Quote-Name $cc.constraint_name) CHECK $($cc.definition);"
}

$indexes = Invoke-Table $OldDatabase @"
SELECT
    s.name AS schema_name,
    t.name AS table_name,
    i.name AS index_name,
    i.is_unique,
    i.type_desc,
    i.has_filter,
    i.filter_definition,
    ic.key_ordinal,
    ic.is_descending_key,
    ic.is_included_column,
    c.name AS column_name
FROM sys.indexes i
JOIN sys.tables t ON t.object_id = i.object_id
JOIN sys.schemas s ON s.schema_id = t.schema_id
JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
WHERE i.is_primary_key = 0
  AND i.is_unique_constraint = 0
  AND i.type > 0
  AND i.name IS NOT NULL
  AND t.is_ms_shipped = 0
ORDER BY s.name, t.name, i.name, ic.key_ordinal, ic.index_column_id
"@
foreach ($group in ($indexes | Group-Object schema_name, table_name, index_name)) {
    $first = $group.Group[0]
    $keys = $group.Group | Where-Object { -not [bool]$_.is_included_column } | Sort-Object key_ordinal
    if ($keys.Count -eq 0) { continue }
    $keyText = ($keys | ForEach-Object { "$(Quote-Name $_.column_name) " + ($(if ([bool]$_.is_descending_key) { "DESC" } else { "ASC" })) }) -join ", "
    $include = $group.Group | Where-Object { [bool]$_.is_included_column } | ForEach-Object { Quote-Name $_.column_name }
    $unique = if ([bool]$first.is_unique) { "UNIQUE " } else { "" }
    $indexType = if ([string]$first.type_desc -eq "CLUSTERED") { "CLUSTERED" } else { "NONCLUSTERED" }
    $sql = "CREATE $unique$indexType INDEX $(Quote-Name $first.index_name) ON $(Quote-Name $first.schema_name).$(Quote-Name $first.table_name) ($keyText)"
    if ($include.Count -gt 0) { $sql += " INCLUDE (" + ($include -join ", ") + ")" }
    if ([bool]$first.has_filter) { $sql += " WHERE $($first.filter_definition)" }
    Invoke-NonQuery $NewDatabase "$sql;"
}

foreach ($table in $tables) {
    $schemaName = [string]$table.schema_name
    $tableName = [string]$table.table_name
    $tableColumns = $columns | Where-Object { $_.schema_name -eq $schemaName -and $_.table_name -eq $tableName -and -not [bool]$_.is_computed }
    $identityCol = $null
    foreach ($col in $tableColumns) {
        $key = "$schemaName.$tableName.$($col.column_name)"
        if ([bool]$col.is_identity -or $identityCandidates.ContainsKey($key)) { $identityCol = $col.column_name; break }
    }
    if ($identityCol) {
        Invoke-NonQuery $NewDatabase "DBCC CHECKIDENT ('$(Quote-Name $schemaName).$(Quote-Name $tableName)', RESEED) WITH NO_INFOMSGS;"
    }
}

$oldCounts = Invoke-Table $OldDatabase "SELECT s.name AS schema_name, t.name AS table_name, SUM(p.rows) AS row_count FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id JOIN sys.partitions p ON p.object_id=t.object_id AND p.index_id IN (0,1) WHERE t.is_ms_shipped=0 GROUP BY s.name,t.name"
$newCounts = Invoke-Table $NewDatabase "SELECT s.name AS schema_name, t.name AS table_name, SUM(p.rows) AS row_count FROM sys.tables t JOIN sys.schemas s ON s.schema_id=t.schema_id JOIN sys.partitions p ON p.object_id=t.object_id AND p.index_id IN (0,1) WHERE t.is_ms_shipped=0 GROUP BY s.name,t.name"

Write-Output "Identity converted columns:"
foreach ($key in ($identityCandidates.Keys | Sort-Object)) { Write-Output "  $key" }

Write-Output "Record counts:"
foreach ($old in $oldCounts) {
    $new = $newCounts | Where-Object { $_.schema_name -eq $old.schema_name -and $_.table_name -eq $old.table_name } | Select-Object -First 1
    $newCount = if ($new) { [int64]$new.row_count } else { -1 }
    Write-Output "  $($old.schema_name).$($old.table_name): old=$($old.row_count), new=$newCount"
    if ([int64]$old.row_count -ne $newCount) {
        throw "Record count mismatch for $($old.schema_name).$($old.table_name)"
    }
}

$fkViolations = Invoke-Table $NewDatabase @"
DBCC CHECKCONSTRAINTS WITH ALL_CONSTRAINTS
"@
if ($fkViolations.Rows.Count -gt 0) {
    $fkViolations | Format-Table | Out-String | Write-Output
    throw "Constraint check failed in $NewDatabase"
}

Write-Output "Migration completed successfully."
