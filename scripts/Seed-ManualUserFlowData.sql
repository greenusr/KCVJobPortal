SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

/* Profile edit flow: candidates add work experience from Profile/Edit. */
IF NOT EXISTS (
    SELECT 1
    FROM dbo.UserExperiences
    WHERE user_id = 16
      AND company_name = N'GreenTech Labs'
      AND position = N'Frontend Developer'
)
BEGIN
    INSERT INTO dbo.UserExperiences
        (user_id, company_name, position, start_date, end_date, description)
    VALUES
        (16, N'GreenTech Labs', N'Frontend Developer', '2023-01-01', '2024-04-30',
         N'Built responsive career pages, reusable UI components, and dashboard widgets for recruitment campaigns.');
END;

IF NOT EXISTS (
    SELECT 1
    FROM dbo.UserExperiences
    WHERE user_id = 17
      AND company_name = N'Saigon Digital'
      AND position = N'Backend Developer'
)
BEGIN
    INSERT INTO dbo.UserExperiences
        (user_id, company_name, position, start_date, end_date, description)
    VALUES
        (17, N'Saigon Digital', N'Backend Developer', '2022-06-01', '2024-02-29',
         N'Designed REST APIs, optimized SQL queries, and maintained background jobs for applicant tracking features.');
END;

IF NOT EXISTS (
    SELECT 1
    FROM dbo.UserExperiences
    WHERE user_id = 18
      AND company_name = N'Aurora Product Studio'
      AND position = N'UI/UX Designer'
)
BEGIN
    INSERT INTO dbo.UserExperiences
        (user_id, company_name, position, start_date, end_date, description)
    VALUES
        (18, N'Aurora Product Studio', N'UI/UX Designer', '2023-03-01', NULL,
         N'Created candidate profile flows, design system tokens, and usability test prototypes for hiring products.');
END;

/* Profile edit flow: candidates add portfolio projects from Profile/Edit. */
IF NOT EXISTS (
    SELECT 1
    FROM dbo.UserProjects
    WHERE user_id = 16
      AND project_name = N'KCV Portfolio Tracker'
)
BEGIN
    INSERT INTO dbo.UserProjects
        (user_id, project_name, description, project_url, start_date, end_date)
    VALUES
        (16, N'KCV Portfolio Tracker',
         N'A React-style portfolio dashboard that tracks saved jobs, application status, and recruiter messages.',
         N'https://portfolio.kcv-demo.vn/long-hoang/tracker', '2024-05-01', '2024-08-31');
END;

IF NOT EXISTS (
    SELECT 1
    FROM dbo.UserProjects
    WHERE user_id = 17
      AND project_name = N'Interview Scheduler API'
)
BEGIN
    INSERT INTO dbo.UserProjects
        (user_id, project_name, description, project_url, start_date, end_date)
    VALUES
        (17, N'Interview Scheduler API',
         N'An API service for interview slots, reminders, and recruiter notes with SQL Server persistence.',
         N'https://github.com/kcv-demo/interview-scheduler-api', '2023-09-01', '2024-01-31');
END;

IF NOT EXISTS (
    SELECT 1
    FROM dbo.UserProjects
    WHERE user_id = 18
      AND project_name = N'Candidate Profile Redesign'
)
BEGIN
    INSERT INTO dbo.UserProjects
        (user_id, project_name, description, project_url, start_date, end_date)
    VALUES
        (18, N'Candidate Profile Redesign',
         N'A UX case study that improves profile completion, skill discovery, and employer invitation conversion.',
         N'https://behance.net/kcv-demo/candidate-profile-redesign', '2024-02-01', '2024-06-30');
END;

/* Employer invite flow: employers invite candidates from profile pages. */
IF NOT EXISTS (
    SELECT 1
    FROM dbo.CandidateInvitations
    WHERE employer_id = 3
      AND candidate_id = 16
      AND job_id = 1
      AND status = N'Pending'
)
BEGIN
    INSERT INTO dbo.CandidateInvitations
        (employer_id, candidate_id, job_id, message, status, created_at)
    VALUES
        (3, 16, 1,
         N'Your frontend portfolio looks strong. We would like to invite you to review this role at FPT Software.',
         N'Pending', DATEADD(day, -1, GETDATE()));
END;

IF NOT EXISTS (
    SELECT 1
    FROM dbo.CandidateInvitations
    WHERE employer_id = 4
      AND candidate_id = 17
      AND job_id = 2
      AND status = N'Accepted'
)
BEGIN
    INSERT INTO dbo.CandidateInvitations
        (employer_id, candidate_id, job_id, message, status, created_at, responded_at)
    VALUES
        (4, 17, 2,
         N'We noticed your backend API experience and think you could be a fit for this VNG backend role.',
         N'Accepted', DATEADD(day, -4, GETDATE()), DATEADD(day, -2, GETDATE()));
END;

IF NOT EXISTS (
    SELECT 1
    FROM dbo.CandidateInvitations
    WHERE employer_id = 5
      AND candidate_id = 18
      AND job_id = 3
      AND status = N'Declined'
)
BEGIN
    INSERT INTO dbo.CandidateInvitations
        (employer_id, candidate_id, job_id, message, status, created_at, responded_at)
    VALUES
        (5, 18, 3,
         N'Your fullstack and product mindset stood out. Please consider this opportunity with Viettel Group.',
         N'Declined', DATEADD(day, -5, GETDATE()), DATEADD(day, -3, GETDATE()));
END;

DECLARE @SeededInvitations TABLE
(
    invitation_id INT NOT NULL,
    candidate_id INT NOT NULL,
    job_title NVARCHAR(100) NULL,
    status NVARCHAR(30) NOT NULL
);

INSERT INTO @SeededInvitations (invitation_id, candidate_id, job_title, status)
SELECT invitation.invitation_id, invitation.candidate_id, job.job_title, invitation.status
FROM dbo.CandidateInvitations invitation
JOIN dbo.Jobs job ON job.job_id = invitation.job_id
WHERE (invitation.employer_id = 3 AND invitation.candidate_id = 16 AND invitation.job_id = 1 AND invitation.status = N'Pending')
   OR (invitation.employer_id = 4 AND invitation.candidate_id = 17 AND invitation.job_id = 2 AND invitation.status = N'Accepted')
   OR (invitation.employer_id = 5 AND invitation.candidate_id = 18 AND invitation.job_id = 3 AND invitation.status = N'Declined');

/* Invitation flow also creates an in-app notification for the candidate. */
INSERT INTO dbo.Notifications
    (user_id, title, message, type, related_id, related_type, is_read, created_at)
SELECT seeded.candidate_id,
       N'Job invitation',
       N'You have been invited to apply for ' + ISNULL(seeded.job_title, N'this job') + N'.',
       N'Invitation',
       seeded.invitation_id,
       N'Invitation',
       0,
       GETDATE()
FROM @SeededInvitations seeded
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.Notifications existing
    WHERE existing.user_id = seeded.candidate_id
      AND existing.type = N'Invitation'
      AND existing.related_type = N'Invitation'
      AND existing.related_id = seeded.invitation_id
);

/* Candidate response flow writes activity log rows for accepted/declined invitations. */
INSERT INTO dbo.UserActivityLogs
    (user_id, action, description, related_id, related_type, created_at)
SELECT seeded.candidate_id,
       CASE seeded.status WHEN N'Accepted' THEN N'AcceptInvitation' ELSE N'DeclineInvitation' END,
       CASE seeded.status WHEN N'Accepted' THEN N'Invitation accepted.' ELSE N'Invitation declined.' END,
       seeded.invitation_id,
       N'Invitation',
       GETDATE()
FROM @SeededInvitations seeded
WHERE seeded.status IN (N'Accepted', N'Declined')
  AND NOT EXISTS (
      SELECT 1
      FROM dbo.UserActivityLogs existing
      WHERE existing.user_id = seeded.candidate_id
        AND existing.related_type = N'Invitation'
        AND existing.related_id = seeded.invitation_id
        AND existing.action = CASE seeded.status WHEN N'Accepted' THEN N'AcceptInvitation' ELSE N'DeclineInvitation' END
  );

COMMIT TRANSACTION;

SELECT 'CandidateInvitations' AS table_name, COUNT(*) AS row_count FROM dbo.CandidateInvitations
UNION ALL
SELECT 'UserExperiences', COUNT(*) FROM dbo.UserExperiences
UNION ALL
SELECT 'UserProjects', COUNT(*) FROM dbo.UserProjects;
