using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using JobPortalKCV.Models;
using JobPortalKCV.Models.ViewModel;
using JobPortalKCV.Services;

namespace JobPortalKCV.Controllers
{
    [Authorize]
    public class AdminController : Controller
    {
        private readonly JobPortalDataContext data = new JobPortalDataContext();

        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (!UserIsInRole("Admin"))
            {
                filterContext.Result = new HttpStatusCodeResult(403, "You are not allowed to access this page.");
                return;
            }

            base.OnActionExecuting(filterContext);
        }

        public ActionResult Index()
        {
            new UserVerificationService(data);

            var verifiedUserIds = data.UserVerifications
                .Where(verification =>
                    verification.type == UserVerificationService.RegisterType &&
                    verification.is_verified)
                .Select(verification => verification.user_id)
                .Distinct()
                .ToList();

            var model = new AdminDashboardViewModel
            {
                TotalUsers = data.Users.Count(),
                TotalCompanies = data.Companies.Count(),
                TotalJobs = data.Jobs.Count(),
                TotalApplications = data.JobApplications.Count(),
                PendingApplications = data.JobApplications.Count(application => application.status == "Pending" || application.status == null),
                TotalStars = data.Stars.Count(),
                VerifiedUsers = verifiedUserIds.Count,
                PendingEmailUsers = data.Users.Count() - verifiedUserIds.Count,
                CandidateUsers = CountUsersInRole("Candidate"),
                EmployerUsers = CountUsersInRole("Employer"),
                Tables = GetTableDefinitions()
                    .Select(table => new AdminTableLinkViewModel
                    {
                        TableName = table.TableName,
                        DisplayName = table.DisplayName,
                        RecordCount = CountRows(table.TableName)
                    })
                    .ToList(),
                RecentUsers = GetRecentUsers(),
                RecentJobs = GetRecentJobs(),
                RecentApplications = GetRecentApplications(),
                RecentLogs = GetRecentLogs(),
                ApplicationsByStatus = GetApplicationsByStatus(),
                JobsByCategory = GetJobsByCategory(),
                UsersByRole = GetUsersByRole()
            };

            return View(model);
        }

        private List<AdminRecentUserViewModel> GetRecentUsers()
        {
            return (from user in data.Users
                    join profile in data.UserProfileRecords on user.user_id equals profile.user_id into profileJoin
                    from profile in profileJoin.DefaultIfEmpty()
                    orderby user.user_id descending
                    select new
                    {
                        User = user,
                        Profile = profile
                    })
                .Take(5)
                .ToList()
                .Select(item => new AdminRecentUserViewModel
                {
                    UserId = item.User.user_id,
                    DisplayName = FirstNotEmpty(item.Profile?.full_name, item.User.username),
                    Email = item.User.email,
                    RoleName = data.UserRoles.Where(userRole => userRole.user_id == item.User.user_id).Select(userRole => userRole.Role.role_name).FirstOrDefault()
                })
                .ToList();
        }

        private List<AdminRecentJobViewModel> GetRecentJobs()
        {
            return (from job in data.Jobs
                    join company in data.Companies on job.company_id equals company.company_id
                    orderby job.posted_date descending, job.job_id descending
                    select new
                    {
                        Job = job,
                        Company = company
                    })
                .Take(5)
                .ToList()
                .Select(item => new AdminRecentJobViewModel
                    {
                        JobId = item.Job.job_id,
                        JobTitle = item.Job.job_title,
                        CompanyName = item.Company.company_name,
                        PostedDate = item.Job.posted_date.HasValue ? item.Job.posted_date.Value.ToString("dd/MM/yyyy") : ""
                    }).ToList();
        }

        private List<AdminRecentApplicationViewModel> GetRecentApplications()
        {
            return (from application in data.JobApplications
                    join job in data.Jobs on application.job_id equals job.job_id
                    join user in data.Users on application.user_id equals user.user_id
                    orderby application.applied_date descending, application.application_date descending, application.application_id descending
                    select new
                    {
                        Application = application,
                        Job = job,
                        User = user
                    })
                .Take(5)
                .ToList()
                .Select(item => new AdminRecentApplicationViewModel
                    {
                        ApplicationId = item.Application.application_id,
                        JobTitle = item.Job.job_title,
                        ApplicantName = GetUserDisplayName(item.User),
                        Status = String.IsNullOrWhiteSpace(item.Application.status) ? "Pending" : item.Application.status,
                        AppliedDate = (item.Application.applied_date ?? item.Application.application_date).HasValue ? (item.Application.applied_date ?? item.Application.application_date).Value.ToString("dd/MM/yyyy HH:mm") : ""
                    }).ToList();
        }

        private List<AdminRecentLogViewModel> GetRecentLogs()
        {
            return data.UserActivityLogs
                .OrderByDescending(log => log.created_at)
                .Take(5)
                .ToList()
                .Select(log => new AdminRecentLogViewModel
                {
                    Action = log.action,
                    Description = log.description,
                    CreatedAt = log.created_at.ToString("dd/MM/yyyy HH:mm")
                }).ToList();
        }

        private List<AdminChartItemViewModel> GetApplicationsByStatus()
        {
            var items = data.JobApplications
                .ToList()
                .GroupBy(application => String.IsNullOrWhiteSpace(application.status) ? "Pending" : application.status)
                .Select(group => new AdminChartItemViewModel { Label = group.Key, Count = group.Count() })
                .OrderByDescending(item => item.Count)
                .ToList();

            return WithPercent(items);
        }

        private List<AdminChartItemViewModel> GetJobsByCategory()
        {
            var items = (from map in data.JobCategoryMaps
                         join category in data.JobCategories on map.category_id equals category.category_id
                         group map by category.category_name into categoryGroup
                         orderby categoryGroup.Count() descending
                         select new AdminChartItemViewModel
                         {
                             Label = categoryGroup.Key,
                             Count = categoryGroup.Count()
                         }).Take(6).ToList();

            return WithPercent(items);
        }

        private List<AdminChartItemViewModel> GetUsersByRole()
        {
            var items = (from userRole in data.UserRoles
                         join role in data.Roles on userRole.role_id equals role.role_id
                         group userRole by role.role_name into roleGroup
                         orderby roleGroup.Count() descending
                         select new AdminChartItemViewModel
                         {
                             Label = roleGroup.Key,
                             Count = roleGroup.Count()
                         }).ToList();

            return WithPercent(items);
        }

        private List<AdminChartItemViewModel> WithPercent(List<AdminChartItemViewModel> items)
        {
            var max = items.Any() ? items.Max(item => item.Count) : 0;

            foreach (var item in items)
                item.Percent = max == 0 ? 0 : Math.Max(6, (int)Math.Round(item.Count * 100.0 / max));

            return items;
        }

        public ActionResult Table(string table, string keyword, string sort, int page = 1)
        {
            var definition = GetTableDefinition(table);

            if (definition == null)
                return HttpNotFound();

            page = Math.Max(1, page);
            var pageSize = SystemSettingsService.GetPaginationSize(data);
            var columns = GetColumns(definition.TableName)
                .Where(column => !definition.HiddenColumns.Contains(column.Name))
                .ToList();
            var selectableColumns = columns.Select(column => Quote(column.Name)).ToList();
            var parameters = new List<SqlParameter>();
            var whereClause = BuildSearchWhere(definition, keyword, parameters);
            var orderClause = BuildOrderClause(definition, columns, sort);
            var total = CountRows(definition.TableName, whereClause, parameters);
            var rows = ReadRows(definition, columns, whereClause, orderClause, page, pageSize, parameters);

            return View(new AdminTableListViewModel
            {
                TableName = definition.TableName,
                DisplayName = definition.DisplayName,
                Keyword = keyword,
                Sort = sort,
                Page = page,
                PageSize = pageSize,
                TotalRecords = total,
                CanCreate = definition.CanCreate,
                CanEdit = definition.CanEdit,
                CanDelete = definition.CanDelete,
                DeleteButtonText = definition.DisableMode == "User" ? "Disable" : "Delete",
                Columns = selectableColumns.Select(column => column.Trim('[', ']')).ToList(),
                Rows = rows
            });
        }

        public ActionResult Details(string table, string key)
        {
            var definition = GetTableDefinition(table);

            if (definition == null)
                return HttpNotFound();

            var row = GetRecord(definition, key);

            if (row == null)
                return HttpNotFound();

            return View(BuildFormModel(definition, key, row, false, false));
        }

        public ActionResult Create(string table)
        {
            var definition = GetTableDefinition(table);

            if (definition == null || !definition.CanCreate)
                return HttpNotFound();

            return View("Form", BuildFormModel(definition, null, new Dictionary<string, string>(), true, true));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(string table, FormCollection form, string returnUrl = null)
        {
            var definition = GetTableDefinition(table);

            if (definition == null || !definition.CanCreate)
                return HttpNotFound();

            try
            {
                CreateRecord(definition, form);
                TempData["AdminMessage"] = "Changes saved successfully.";
                return RedirectToAdminContext(definition.TableName, returnUrl);
            }
            catch
            {
                TempData["AdminError"] = "Something went wrong. Please try again.";
                return View("Form", BuildFormModel(definition, null, FormToDictionary(form), true, true));
            }
        }

        public ActionResult Edit(string table, string key)
        {
            var definition = GetTableDefinition(table);

            if (definition == null)
                return HttpNotFound();

            var row = GetRecord(definition, key);

            if (row == null)
                return HttpNotFound();

            if (!definition.CanEdit)
            {
                TempData["AdminError"] = "Logs cannot be modified.";
                return View("Form", BuildFormModel(definition, key, row, false, false));
            }

            return View("Form", BuildFormModel(definition, key, row, false, true));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(string table, string key, FormCollection form, string returnUrl = null)
        {
            var definition = GetTableDefinition(table);

            if (definition == null)
                return HttpNotFound();

            if (!definition.CanEdit)
            {
                TempData["AdminError"] = "Logs cannot be modified.";
                return RedirectToAdminContext(definition.TableName, returnUrl);
            }

            try
            {
                UpdateRecord(definition, key, form);
                TempData["AdminMessage"] = "Record updated successfully.";
                return RedirectToAdminContext(definition.TableName, returnUrl);
            }
            catch
            {
                TempData["AdminError"] = "Something went wrong. Please try again.";
                return View("Form", BuildFormModel(definition, key, FormToDictionary(form), false, true));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(string table, string key, string returnUrl = null)
        {
            var definition = GetTableDefinition(table);

            if (definition == null)
                return HttpNotFound();

            if (!definition.CanDelete)
            {
                TempData["AdminError"] = "You are not allowed to access this page.";
                return RedirectToAdminContext(table, returnUrl);
            }

            try
            {
                if (definition.DisableMode == "User")
                {
                    ExecuteNonQuery("UPDATE dbo.Users SET is_active = 0 WHERE user_id = @id", new SqlParameter("@id", ParseKey(key)["user_id"]));
                    TempData["AdminMessage"] = "Record disabled successfully.";
                }
                else
                {
                    DeleteRecord(definition, key);
                    TempData["AdminMessage"] = "Record deleted successfully.";
                }
            }
            catch (Exception ex)
            {
                TempData["AdminError"] = "Something went wrong. Please try again. " + ex.Message;
            }

            return RedirectToAdminContext(definition.TableName, returnUrl);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EnableUser(string key, string returnUrl = null)
        {
            var definition = GetTableDefinition("Users");

            try
            {
                ExecuteNonQuery("UPDATE dbo.Users SET is_active = 1 WHERE user_id = @id", new SqlParameter("@id", ParseKey(key)["user_id"]));
                TempData["AdminMessage"] = "Record updated successfully.";
            }
            catch
            {
                TempData["AdminError"] = "Something went wrong. Please try again.";
            }

            return RedirectToAdminContext(definition.TableName, returnUrl);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                data.Dispose();

            base.Dispose(disposing);
        }

        private bool UserIsInRole(string roleName)
        {
            return data.UserRoles.Any(userRole =>
                userRole.User.username == User.Identity.Name &&
                userRole.Role.role_name == roleName);
        }

        private ActionResult RedirectToAdminContext(string table, string returnUrl)
        {
            if (!String.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);

            return RedirectToAction("Table", new { table = table });
        }

        private int CountUsersInRole(string roleName)
        {
            return data.UserRoles.Count(userRole => userRole.Role.role_name == roleName);
        }

        private string FirstNotEmpty(params string[] values)
        {
            return values.FirstOrDefault(value => !String.IsNullOrWhiteSpace(value));
        }

        private string GetUserDisplayName(User user)
        {
            if (user == null)
                return "";

            var profileName = data.UserProfileRecords
                .Where(profile => profile.user_id == user.user_id)
                .Select(profile => profile.full_name)
                .FirstOrDefault();

            return FirstNotEmpty(profileName, user.username);
        }

        private List<AdminTableDefinition> GetTableDefinitions()
        {
            return new List<AdminTableDefinition>
            {
                new AdminTableDefinition("Users", "Users", new[] { "user_id" }, new[] { "password_hash" }, new[] { "user_id", "password_hash" }, new[] { "username", "email" }, true, "User"),
                new AdminTableDefinition("Roles", "Roles", new[] { "role_id" }, null, new[] { "role_id" }, new[] { "role_name" }, true, null),
                new AdminTableDefinition("Companies", "Companies", new[] { "company_id" }, null, new[] { "company_id" }, new[] { "company_name", "industry", "website", "contact_email", "description" }, true, null),
                new AdminTableDefinition("CompanyUsers", "Company Users", new[] { "user_id", "company_id" }, null, new string[0], new[] { "role" }, true, null),
                new AdminTableDefinition("Jobs", "Jobs", new[] { "job_id" }, null, new[] { "job_id" }, new[] { "job_title", "job_description", "salary_range" }, true, null),
                new AdminTableDefinition("JobCategories", "Job Categories", new[] { "category_id" }, null, new[] { "category_id" }, new[] { "category_name" }, true, null),
                new AdminTableDefinition("JobSkills", "Job Skills", new[] { "job_skill_id" }, null, new[] { "job_skill_id" }, new[] { "job_id", "skill_id" }, true, null),
                new AdminTableDefinition("Skills", "Skills", new[] { "skill_id" }, null, new[] { "skill_id" }, new[] { "skill_name" }, true, null),
                new AdminTableDefinition("UserProfiles", "User Profiles", new[] { "profile_id" }, null, new[] { "profile_id", "created_at", "updated_at" }, new[] { "full_name", "phone", "address", "about_me" }, true, null),
                new AdminTableDefinition("UserEducations", "User Educations", new[] { "education_id" }, null, new[] { "education_id" }, new[] { "school_name", "degree", "field_of_study", "description" }, true, null),
                new AdminTableDefinition("UserExperiences", "User Experiences", new[] { "experience_id" }, null, new[] { "experience_id" }, new[] { "company_name", "position", "description" }, true, null),
                new AdminTableDefinition("UserProjects", "User Projects", new[] { "project_id" }, null, new[] { "project_id" }, new[] { "project_name", "description", "project_url" }, true, null),
                new AdminTableDefinition("UserSkills", "User Skills", new[] { "user_skill_id" }, null, new[] { "user_skill_id" }, new[] { "user_id", "skill_id" }, true, null),
                new AdminTableDefinition("UserCVs", "User CVs", new[] { "cv_id" }, null, new[] { "cv_id", "created_at" }, new[] { "file_name", "file_path" }, true, null),
                new AdminTableDefinition("JobApplications", "Job Applications", new[] { "application_id" }, null, new[] { "application_id" }, new[] { "cover_letter", "status", "final_result" }, true, null),
                new AdminTableDefinition("Interviews", "Interviews", new[] { "interview_id" }, null, new[] { "interview_id", "created_at" }, new[] { "location", "contact_name", "contact_email", "contact_phone", "additional_info" }, true, null),
                new AdminTableDefinition("Stars", "Stars", new[] { "star_id" }, null, new[] { "star_id", "created_at" }, new[] { "target_type" }, true, null),
                new AdminTableDefinition("CandidateInvitations", "Candidate Invitations", new[] { "invitation_id" }, null, new[] { "invitation_id", "created_at" }, new[] { "message", "status" }, true, null),
                new AdminTableDefinition("UserVerifications", "User Verifications", new[] { "verification_id" }, new[] { "otp_code" }, new[] { "verification_id", "otp_code", "created_at" }, new[] { "type" }, true, null),
                new AdminTableDefinition("ApplicationStatuses", "Application Statuses", new[] { "status_id" }, null, new[] { "status_id" }, new[] { "status_name" }, true, null),
                new AdminTableDefinition("CompanyJoinRequests", "Company Join Requests", new[] { "request_id" }, null, new[] { "request_id", "requested_at" }, new[] { "status" }, true, null),
                new AdminTableDefinition("EmploymentTypes", "Employment Types", new[] { "employment_type_id" }, null, new[] { "employment_type_id" }, new[] { "type_name" }, true, null),
                new AdminTableDefinition("JobCategoryMap", "Job Category Map", new[] { "job_id", "category_id" }, null, new string[0], new[] { "job_id", "category_id" }, true, null, false),
                new AdminTableDefinition("Locations", "Locations", new[] { "location_id" }, null, new[] { "location_id" }, new[] { "country", "city" }, true, null),
                new AdminTableDefinition("Notifications", "Notifications", new[] { "notification_id" }, null, new[] { "notification_id", "created_at" }, new[] { "title", "message", "type", "related_type" }, true, null, true, true),
                new AdminTableDefinition("SystemSettings", "System Settings", new[] { "setting_id" }, null, new[] { "setting_id", "created_at", "updated_at" }, new[] { "site_name", "allowed_image_types", "allowed_cv_types" }, false, null, true, true),
                new AdminTableDefinition("UserActivityLogs", "User Activity Logs", new[] { "activity_log_id" }, null, new[] { "activity_log_id", "user_id", "action", "description", "keyword", "filters", "related_id", "related_type", "ip_address", "user_agent", "created_at" }, new[] { "action", "description", "keyword", "filters", "related_type" }, false, null, false, true),
                new AdminTableDefinition("UserLoginLogs", "User Login Logs", new[] { "login_log_id" }, null, new[] { "login_log_id", "user_id", "login_time", "ip_address", "user_agent", "is_success", "failure_reason" }, new[] { "ip_address", "user_agent", "failure_reason" }, false, null, false, true),
                new AdminTableDefinition("UserRoles", "User Roles", new[] { "user_id", "role_id" }, null, new string[0], new[] { "user_id", "role_id" }, true, null, false),
                new AdminTableDefinition("UserSettings", "User Settings", new[] { "setting_id" }, null, new[] { "setting_id", "user_id", "created_at", "updated_at" }, new[] { "user_id" }, true, null, true, true)
            };
        }

        private AdminTableDefinition GetTableDefinition(string table)
        {
            return GetTableDefinitions().FirstOrDefault(item => String.Equals(item.TableName, table, StringComparison.OrdinalIgnoreCase));
        }

        private List<AdminColumnDefinition> GetColumns(string tableName)
        {
            var result = new List<AdminColumnDefinition>();

            using (var connection = new SqlConnection(data.Connection.ConnectionString))
            using (var command = new SqlCommand(@"
SELECT COLUMN_NAME, DATA_TYPE, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_SCHEMA = 'dbo' AND TABLE_NAME = @table
ORDER BY ORDINAL_POSITION", connection))
            {
                command.Parameters.AddWithValue("@table", tableName);
                connection.Open();

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new AdminColumnDefinition
                        {
                            Name = reader.GetString(0),
                            DataType = reader.GetString(1),
                            IsNullable = reader.GetString(2) == "YES"
                        });
                    }
                }
            }

            return result;
        }

        private int CountRows(string tableName)
        {
            return CountRows(tableName, "", new List<SqlParameter>());
        }

        private int CountRows(string tableName, string whereClause, List<SqlParameter> parameters)
        {
            using (var connection = new SqlConnection(data.Connection.ConnectionString))
            using (var command = new SqlCommand("SELECT COUNT(*) FROM dbo." + Quote(tableName) + " " + whereClause, connection))
            {
                command.Parameters.AddRange(parameters.Select(parameter => new SqlParameter(parameter.ParameterName, parameter.Value)).ToArray());
                connection.Open();
                return Convert.ToInt32(command.ExecuteScalar());
            }
        }

        private List<Dictionary<string, string>> ReadRows(AdminTableDefinition definition, List<AdminColumnDefinition> columns, string whereClause, string orderClause, int page, int pageSize, List<SqlParameter> parameters)
        {
            var columnSql = String.Join(", ", columns.Select(column => Quote(column.Name)));
            var sql = "SELECT " + columnSql + " FROM dbo." + Quote(definition.TableName) + " " + whereClause + " " + orderClause + " OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";
            var rows = new List<Dictionary<string, string>>();

            using (var connection = new SqlConnection(data.Connection.ConnectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddRange(parameters.Select(parameter => new SqlParameter(parameter.ParameterName, parameter.Value)).ToArray());
                command.Parameters.AddWithValue("@offset", (page - 1) * pageSize);
                command.Parameters.AddWithValue("@pageSize", pageSize);
                connection.Open();

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var row = new Dictionary<string, string>();

                        foreach (var column in columns)
                            row[column.Name] = FormatValue(reader[column.Name], column.DataType);

                        row["_key"] = BuildKey(definition, row);
                        rows.Add(row);
                    }
                }
            }

            return rows;
        }

        private string BuildSearchWhere(AdminTableDefinition definition, string keyword, List<SqlParameter> parameters)
        {
            if (String.IsNullOrWhiteSpace(keyword))
                return "";

            var searchColumns = definition.SearchColumns.Where(column => GetColumns(definition.TableName).Any(actual => actual.Name == column)).ToList();

            if (!searchColumns.Any())
                return "";

            parameters.Add(new SqlParameter("@keyword", "%" + keyword.Trim() + "%"));
            return "WHERE " + String.Join(" OR ", searchColumns.Select(column => "CONVERT(NVARCHAR(MAX), " + Quote(column) + ") LIKE @keyword"));
        }

        private string BuildOrderClause(AdminTableDefinition definition, List<AdminColumnDefinition> columns, string sort)
        {
            var sortColumn = definition.KeyColumns.First();
            var direction = "ASC";

            if (!String.IsNullOrWhiteSpace(sort))
            {
                var parts = sort.Split(':');

                if (parts.Length > 0 && columns.Any(column => column.Name == parts[0]))
                    sortColumn = parts[0];

                if (parts.Length > 1 && parts[1].Equals("desc", StringComparison.OrdinalIgnoreCase))
                    direction = "DESC";
            }

            return "ORDER BY " + Quote(sortColumn) + " " + direction;
        }

        private Dictionary<string, string> GetRecord(AdminTableDefinition definition, string key)
        {
            var columns = GetColumns(definition.TableName)
                .Where(column => !definition.HiddenColumns.Contains(column.Name))
                .ToList();
            var parameters = new List<SqlParameter>();
            var whereClause = BuildKeyWhere(definition, key, parameters);
            var columnSql = String.Join(", ", columns.Select(column => Quote(column.Name)));
            var sql = "SELECT " + columnSql + " FROM dbo." + Quote(definition.TableName) + " WHERE " + whereClause;

            using (var connection = new SqlConnection(data.Connection.ConnectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddRange(parameters.ToArray());
                connection.Open();

                using (var reader = command.ExecuteReader())
                {
                    if (!reader.Read())
                        return null;

                    var row = new Dictionary<string, string>();

                    foreach (var column in columns)
                        row[column.Name] = FormatValue(reader[column.Name], column.DataType);

                    return row;
                }
            }
        }

        private AdminRecordFormViewModel BuildFormModel(AdminTableDefinition definition, string key, Dictionary<string, string> values, bool isCreate, bool editable)
        {
            var columns = GetColumns(definition.TableName)
                .Where(column => !definition.HiddenColumns.Contains(column.Name))
                .ToList();

            return new AdminRecordFormViewModel
            {
                TableName = definition.TableName,
                DisplayName = definition.DisplayName,
                Key = key,
                IsCreate = isCreate,
                CanEdit = definition.CanEdit,
                Fields = columns.Select(column =>
                {
                    var value = values.ContainsKey(column.Name) ? values[column.Name] : "";
                    var options = GetLookupOptions(column.Name);
                    var selectedOption = options.FirstOrDefault(option => option.Value == value);

                    return new AdminFieldViewModel
                    {
                        Name = column.Name,
                        Label = CultureInfo.CurrentCulture.TextInfo.ToTitleCase(column.Name.Replace("_", " ")),
                        Value = value,
                        DisplayValue = selectedOption == null ? value : selectedOption.Text,
                        DataType = column.DataType,
                        IsRequired = !column.IsNullable && (isCreate || !definition.KeyColumns.Contains(column.Name)),
                        IsEditable = definition.CanEdit && editable && !definition.ReadOnlyColumns.Contains(column.Name) && (isCreate || !definition.KeyColumns.Contains(column.Name)),
                        Options = options
                    };
                }).ToList()
            };
        }

        private List<AdminSelectOptionViewModel> GetLookupOptions(string columnName)
        {
            var sql = GetLookupSql(columnName);
            var options = new List<AdminSelectOptionViewModel>();

            if (String.IsNullOrWhiteSpace(sql))
                return options;

            using (var connection = new SqlConnection(data.Connection.ConnectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                connection.Open();

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        options.Add(new AdminSelectOptionViewModel
                        {
                            Value = reader["Value"].ToString(),
                            Text = reader["Text"].ToString()
                        });
                    }
                }
            }

            return options;
        }

        private string GetLookupSql(string columnName)
        {
            switch (columnName)
            {
                case "user_id":
                case "employer_id":
                case "candidate_id":
                case "responded_by":
                    return @"
SELECT TOP 300
    CAST(user_id AS NVARCHAR(50)) AS Value,
    'ID ' + CAST(user_id AS NVARCHAR(50)) + ' | ' + ISNULL(NULLIF(username, ''), '(no username)') +
        CASE WHEN email IS NULL OR email = '' THEN '' ELSE ' | ' + email END +
        CASE WHEN p.full_name IS NULL OR p.full_name = '' THEN '' ELSE ' | ' + p.full_name END AS Text
FROM dbo.Users u
LEFT JOIN dbo.UserProfiles p ON u.user_id = p.user_id
ORDER BY u.user_id DESC";
                case "company_id":
                    return @"
SELECT TOP 300
    CAST(company_id AS NVARCHAR(50)) AS Value,
    'ID ' + CAST(company_id AS NVARCHAR(50)) + ' | ' + ISNULL(NULLIF(company_name, ''), '(no company name)') +
        CASE WHEN industry IS NULL OR industry = '' THEN '' ELSE ' | ' + industry END +
        CASE WHEN contact_email IS NULL OR contact_email = '' THEN '' ELSE ' | ' + contact_email END AS Text
FROM dbo.Companies
ORDER BY company_name, company_id";
                case "job_id":
                    return @"
SELECT TOP 300
    CAST(j.job_id AS NVARCHAR(50)) AS Value,
    'ID ' + CAST(j.job_id AS NVARCHAR(50)) + ' | ' + ISNULL(NULLIF(j.job_title, ''), '(no job title)') +
        CASE WHEN c.company_name IS NULL OR c.company_name = '' THEN '' ELSE ' | ' + c.company_name END +
        CASE WHEN l.city IS NULL OR l.city = '' THEN '' ELSE ' | ' + l.city END AS Text
FROM dbo.Jobs j
LEFT JOIN dbo.Companies c ON j.company_id = c.company_id
LEFT JOIN dbo.Locations l ON j.location_id = l.location_id
ORDER BY j.job_title, j.job_id";
                case "skill_id":
                    return @"
SELECT TOP 300
    CAST(skill_id AS NVARCHAR(50)) AS Value,
    'ID ' + CAST(skill_id AS NVARCHAR(50)) + ' | ' + ISNULL(NULLIF(skill_name, ''), '(no skill name)') AS Text
FROM dbo.Skills
ORDER BY skill_name, skill_id";
                case "role_id":
                    return @"
SELECT TOP 300
    CAST(role_id AS NVARCHAR(50)) AS Value,
    'ID ' + CAST(role_id AS NVARCHAR(50)) + ' | ' + ISNULL(NULLIF(role_name, ''), '(no role name)') AS Text
FROM dbo.Roles
ORDER BY role_name, role_id";
                case "category_id":
                    return @"
SELECT TOP 300
    CAST(category_id AS NVARCHAR(50)) AS Value,
    'ID ' + CAST(category_id AS NVARCHAR(50)) + ' | ' + ISNULL(NULLIF(category_name, ''), '(no category name)') AS Text
FROM dbo.JobCategories
ORDER BY category_name, category_id";
                case "location_id":
                    return @"
SELECT TOP 300
    CAST(location_id AS NVARCHAR(50)) AS Value,
    'ID ' + CAST(location_id AS NVARCHAR(50)) + ' | ' + ISNULL(NULLIF(city, ''), '(no city)') +
        CASE WHEN country IS NULL OR country = '' THEN '' ELSE ' | ' + country END AS Text
FROM dbo.Locations
ORDER BY city, country, location_id";
                case "employment_type_id":
                    return @"
SELECT TOP 300
    CAST(employment_type_id AS NVARCHAR(50)) AS Value,
    'ID ' + CAST(employment_type_id AS NVARCHAR(50)) + ' | ' + ISNULL(NULLIF(type_name, ''), '(no type name)') AS Text
FROM dbo.EmploymentTypes
ORDER BY type_name, employment_type_id";
                case "status_id":
                    return @"
SELECT TOP 300
    CAST(status_id AS NVARCHAR(50)) AS Value,
    'ID ' + CAST(status_id AS NVARCHAR(50)) + ' | ' + ISNULL(NULLIF(status_name, ''), '(no status name)') AS Text
FROM dbo.ApplicationStatuses
ORDER BY status_name, status_id";
                case "cv_id":
                    return @"
SELECT TOP 300
    CAST(cv.cv_id AS NVARCHAR(50)) AS Value,
    'ID ' + CAST(cv.cv_id AS NVARCHAR(50)) + ' | ' + ISNULL(NULLIF(cv.file_name, ''), '(no file name)') +
        CASE WHEN u.username IS NULL OR u.username = '' THEN '' ELSE ' | ' + u.username END +
        CASE WHEN u.email IS NULL OR u.email = '' THEN '' ELSE ' | ' + u.email END AS Text
FROM dbo.UserCVs cv
LEFT JOIN dbo.Users u ON cv.user_id = u.user_id
ORDER BY cv.created_at DESC, cv.cv_id DESC";
                case "application_id":
                    return @"
SELECT TOP 300
    CAST(a.application_id AS NVARCHAR(50)) AS Value,
    'ID ' + CAST(a.application_id AS NVARCHAR(50)) + ' | ' + ISNULL(NULLIF(u.username, ''), '(no user)') +
        CASE WHEN u.email IS NULL OR u.email = '' THEN '' ELSE ' | ' + u.email END +
        ' | ' + ISNULL(NULLIF(j.job_title, ''), '(no job)') +
        CASE WHEN a.status IS NULL OR a.status = '' THEN '' ELSE ' | ' + a.status END AS Text
FROM dbo.JobApplications a
LEFT JOIN dbo.Users u ON a.user_id = u.user_id
LEFT JOIN dbo.Jobs j ON a.job_id = j.job_id
ORDER BY a.application_id DESC";
                default:
                    return null;
            }
        }

        private void CreateRecord(AdminTableDefinition definition, FormCollection form)
        {
            var columns = GetColumns(definition.TableName)
                .Where(column => !definition.HiddenColumns.Contains(column.Name) && !definition.ReadOnlyColumns.Contains(column.Name))
                .ToList();

            InsertOrUpdate(definition, columns, form, null);
        }

        private void UpdateRecord(AdminTableDefinition definition, string key, FormCollection form)
        {
            var columns = GetColumns(definition.TableName)
                .Where(column => !definition.HiddenColumns.Contains(column.Name) && !definition.ReadOnlyColumns.Contains(column.Name) && !definition.KeyColumns.Contains(column.Name))
                .ToList();

            InsertOrUpdate(definition, columns, form, key);
        }

        private void InsertOrUpdate(AdminTableDefinition definition, List<AdminColumnDefinition> columns, FormCollection form, string key)
        {
            var parameters = new List<SqlParameter>();

            if (key == null)
            {
                var insertColumns = columns.Select(column => column.Name).ToList();
                var valueSql = new List<string>();
                var hiddenCreateValues = GetHiddenCreateValues(definition);

                if (definition.KeyColumns.Count == 1 &&
                    definition.ReadOnlyColumns.Contains(definition.KeyColumns[0]) &&
                    IsManualIntegerKey(definition.TableName, definition.KeyColumns[0]))
                {
                    var sequenceName = KeySequenceService.GetSequenceName(definition.TableName, definition.KeyColumns[0]);

                    if (String.IsNullOrWhiteSpace(sequenceName))
                        throw new InvalidOperationException("No sequence is configured for " + definition.TableName + "." + definition.KeyColumns[0]);

                    insertColumns.Insert(0, definition.KeyColumns[0]);
                    valueSql.Add("NEXT VALUE FOR dbo." + KeySequenceService.Quote(sequenceName));
                }

                foreach (var hiddenValue in hiddenCreateValues)
                {
                    var parameterName = "@hidden" + parameters.Count;
                    insertColumns.Add(hiddenValue.Key);
                    valueSql.Add(parameterName);
                    parameters.Add(new SqlParameter(parameterName, hiddenValue.Value));
                }

                for (var i = 0; i < columns.Count; i++)
                {
                    var parameterName = "@p" + i;
                    valueSql.Add(parameterName);
                    parameters.Add(new SqlParameter(parameterName, ConvertFormValue(form[columns[i].Name], columns[i])));
                }

                var sql = "INSERT INTO dbo." + Quote(definition.TableName) + " (" + String.Join(", ", insertColumns.Select(Quote)) + ") VALUES (" + String.Join(", ", valueSql) + ")";
                ExecuteNonQuery(sql, parameters.ToArray());
            }
            else
            {
                var setSql = new List<string>();

                for (var i = 0; i < columns.Count; i++)
                {
                    var parameterName = "@p" + i;
                    setSql.Add(Quote(columns[i].Name) + " = " + parameterName);
                    parameters.Add(new SqlParameter(parameterName, ConvertFormValue(form[columns[i].Name], columns[i])));
                }

                var whereParameters = new List<SqlParameter>();
                var whereClause = BuildKeyWhere(definition, key, whereParameters);
                parameters.AddRange(whereParameters);
                var sql = "UPDATE dbo." + Quote(definition.TableName) + " SET " + String.Join(", ", setSql) + " WHERE " + whereClause;
                ExecuteNonQuery(sql, parameters.ToArray());
            }
        }

        private Dictionary<string, object> GetHiddenCreateValues(AdminTableDefinition definition)
        {
            var values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            if (definition.TableName.Equals("UserVerifications", StringComparison.OrdinalIgnoreCase))
                values["otp_code"] = Guid.NewGuid().ToString("N").Substring(0, 6);

            return values;
        }

        private void DeleteRecord(AdminTableDefinition definition, string key)
        {
            DeleteDependentRows(definition.TableName, ParseKey(key));

            var parameters = new List<SqlParameter>();
            var whereClause = BuildKeyWhere(definition, key, parameters);
            ExecuteNonQuery("DELETE FROM dbo." + Quote(definition.TableName) + " WHERE " + whereClause, parameters.ToArray());
        }

        private void DeleteDependentRows(string tableName, Dictionary<string, string> key)
        {
            switch (tableName)
            {
                case "ApplicationStatuses":
                    DeleteApplicationsByFilter("status_id = @id", new SqlParameter("@id", key["status_id"]));
                    break;
                case "Companies":
                    DeleteJobsByFilter("company_id = @id", new SqlParameter("@id", key["company_id"]));
                    ExecuteNonQuery("DELETE FROM dbo.CompanyJoinRequests WHERE company_id = @id", new SqlParameter("@id", key["company_id"]));
                    ExecuteNonQuery("DELETE FROM dbo.CompanyUsers WHERE company_id = @id", new SqlParameter("@id", key["company_id"]));
                    ExecuteNonQuery("DELETE FROM dbo.Stars WHERE target_type = 'Company' AND target_id = @id", new SqlParameter("@id", key["company_id"]));
                    break;
                case "EmploymentTypes":
                    DeleteJobsByFilter("employment_type_id = @id", new SqlParameter("@id", key["employment_type_id"]));
                    break;
                case "JobApplications":
                    ExecuteNonQuery("DELETE FROM dbo.Interviews WHERE application_id = @id", new SqlParameter("@id", key["application_id"]));
                    break;
                case "JobCategories":
                    ExecuteNonQuery("DELETE FROM dbo.JobCategoryMap WHERE category_id = @id", new SqlParameter("@id", key["category_id"]));
                    break;
                case "Jobs":
                    DeleteJobDependents("job_id = @id", new SqlParameter("@id", key["job_id"]));
                    break;
                case "Locations":
                    DeleteJobsByFilter("location_id = @id", new SqlParameter("@id", key["location_id"]));
                    ExecuteNonQuery("DELETE FROM dbo.UserProfiles WHERE location_id = @id", new SqlParameter("@id", key["location_id"]));
                    break;
                case "Roles":
                    ExecuteNonQuery("DELETE FROM dbo.UserRoles WHERE role_id = @id", new SqlParameter("@id", key["role_id"]));
                    break;
                case "Skills":
                    ExecuteNonQuery("DELETE FROM dbo.JobSkills WHERE skill_id = @id", new SqlParameter("@id", key["skill_id"]));
                    ExecuteNonQuery("DELETE FROM dbo.UserSkills WHERE skill_id = @id", new SqlParameter("@id", key["skill_id"]));
                    break;
                case "UserCVs":
                    ExecuteNonQuery("UPDATE dbo.UserSettings SET default_cv_id = NULL WHERE default_cv_id = @id", new SqlParameter("@id", key["cv_id"]));
                    ExecuteNonQuery("UPDATE dbo.JobApplications SET cv_id = NULL WHERE cv_id = @id", new SqlParameter("@id", key["cv_id"]));
                    break;
            }
        }

        private void DeleteJobsByFilter(string filter, params SqlParameter[] parameters)
        {
            DeleteJobDependents(filter, parameters);
            ExecuteNonQuery("DELETE FROM dbo.Jobs WHERE " + filter, parameters);
        }

        private void DeleteJobDependents(string jobFilter, params SqlParameter[] parameters)
        {
            var jobWhere = "job_id IN (SELECT job_id FROM dbo.Jobs WHERE " + jobFilter + ")";

            ExecuteNonQuery("DELETE FROM dbo.Interviews WHERE application_id IN (SELECT application_id FROM dbo.JobApplications WHERE " + jobWhere + ")", parameters);
            ExecuteNonQuery("DELETE FROM dbo.CandidateInvitations WHERE " + jobWhere, parameters);
            ExecuteNonQuery("DELETE FROM dbo.JobCategoryMap WHERE " + jobWhere, parameters);
            ExecuteNonQuery("DELETE FROM dbo.JobSkills WHERE " + jobWhere, parameters);
            ExecuteNonQuery("DELETE FROM dbo.JobApplications WHERE " + jobWhere, parameters);
            ExecuteNonQuery("DELETE FROM dbo.Stars WHERE target_type = 'Job' AND target_id IN (SELECT job_id FROM dbo.Jobs WHERE " + jobFilter + ")", parameters);
        }

        private void DeleteApplicationsByFilter(string filter, params SqlParameter[] parameters)
        {
            ExecuteNonQuery("DELETE FROM dbo.Interviews WHERE application_id IN (SELECT application_id FROM dbo.JobApplications WHERE " + filter + ")", parameters);
            ExecuteNonQuery("DELETE FROM dbo.JobApplications WHERE " + filter, parameters);
        }

        private string BuildKey(AdminTableDefinition definition, Dictionary<string, string> row)
        {
            return String.Join("|", definition.KeyColumns.Select(column => column + "=" + Uri.EscapeDataString(row.ContainsKey(column) ? row[column] : "")));
        }

        private string BuildKeyWhere(AdminTableDefinition definition, string key, List<SqlParameter> parameters)
        {
            var parsed = ParseKey(key);
            var clauses = new List<string>();

            for (var i = 0; i < definition.KeyColumns.Count; i++)
            {
                var column = definition.KeyColumns[i];
                var parameterName = "@key" + i;
                clauses.Add(Quote(column) + " = " + parameterName);
                parameters.Add(new SqlParameter(parameterName, parsed.ContainsKey(column) ? parsed[column] : ""));
            }

            return String.Join(" AND ", clauses);
        }

        private Dictionary<string, string> ParseKey(string key)
        {
            return (key ?? "")
                .Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Split(new[] { '=' }, 2))
                .Where(parts => parts.Length == 2)
                .ToDictionary(parts => parts[0], parts => Uri.UnescapeDataString(parts[1]));
        }

        private Dictionary<string, string> FormToDictionary(FormCollection form)
        {
            return form.AllKeys.ToDictionary(key => key, key => form[key]);
        }

        private object ConvertFormValue(string value, AdminColumnDefinition column)
        {
            if (String.IsNullOrWhiteSpace(value))
                return column.IsNullable ? (object)DBNull.Value : "";

            if (column.DataType == "bit")
                return value
                    .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Any(item =>
                        item.Equals("true", StringComparison.OrdinalIgnoreCase) ||
                        item.Equals("on", StringComparison.OrdinalIgnoreCase) ||
                        item.Equals("1", StringComparison.OrdinalIgnoreCase));

            if (column.DataType == "int")
                return Int32.Parse(value);

            if (column.DataType == "date" || column.DataType == "datetime")
                return DateTime.Parse(value);

            return value;
        }

        private string FormatValue(object value, string dataType)
        {
            if (value == null || value == DBNull.Value)
                return "";

            if (dataType == "date")
                return Convert.ToDateTime(value).ToString("yyyy-MM-dd");

            if (dataType == "datetime")
                return Convert.ToDateTime(value).ToString("yyyy-MM-dd HH:mm");

            if (dataType == "bit")
                return Convert.ToBoolean(value) ? "true" : "false";

            return value.ToString();
        }

        private void ExecuteNonQuery(string sql, params SqlParameter[] parameters)
        {
            using (var connection = new SqlConnection(data.Connection.ConnectionString))
            using (var command = new SqlCommand(sql, connection))
            {
                command.Parameters.AddRange(parameters.Select(parameter => new SqlParameter(parameter.ParameterName, parameter.Value ?? DBNull.Value)).ToArray());
                connection.Open();
                command.ExecuteNonQuery();
            }
        }

        private bool IsManualIntegerKey(string tableName, string columnName)
        {
            var column = GetColumns(tableName).FirstOrDefault(item => item.Name == columnName);
            if (column == null || column.DataType != "int")
                return false;

            using (var connection = new SqlConnection(data.Connection.ConnectionString))
            using (var command = new SqlCommand("SELECT COLUMNPROPERTY(OBJECT_ID(@tableName), @columnName, 'IsIdentity')", connection))
            {
                command.Parameters.AddWithValue("@tableName", "dbo." + tableName);
                command.Parameters.AddWithValue("@columnName", columnName);
                connection.Open();

                var result = command.ExecuteScalar();
                return result == null || result == DBNull.Value || Convert.ToInt32(result) == 0;
            }
        }

        private string Quote(string identifier)
        {
            return "[" + identifier.Replace("]", "]]") + "]";
        }

        private class AdminTableDefinition
        {
            public AdminTableDefinition(string tableName, string displayName, IEnumerable<string> keyColumns, IEnumerable<string> hiddenColumns, IEnumerable<string> readOnlyColumns, IEnumerable<string> searchColumns, bool canCreate, string disableMode, bool canEdit = true, bool canDelete = true)
            {
                TableName = tableName;
                DisplayName = displayName;
                KeyColumns = keyColumns.ToList();
                HiddenColumns = new HashSet<string>(hiddenColumns ?? new string[0]);
                ReadOnlyColumns = new HashSet<string>(readOnlyColumns ?? new string[0]);
                SearchColumns = (searchColumns ?? new string[0]).ToList();
                CanCreate = canCreate;
                DisableMode = disableMode;
                CanEdit = canEdit;
                CanDelete = canDelete;
            }

            public string TableName { get; private set; }
            public string DisplayName { get; private set; }
            public List<string> KeyColumns { get; private set; }
            public HashSet<string> HiddenColumns { get; private set; }
            public HashSet<string> ReadOnlyColumns { get; private set; }
            public List<string> SearchColumns { get; private set; }
            public bool CanCreate { get; private set; }
            public string DisableMode { get; private set; }
            public bool CanEdit { get; private set; }
            public bool CanDelete { get; private set; }
        }

        private class AdminColumnDefinition
        {
            public string Name { get; set; }
            public string DataType { get; set; }
            public bool IsNullable { get; set; }
        }
    }
}
