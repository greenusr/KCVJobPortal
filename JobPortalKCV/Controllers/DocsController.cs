using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using JobPortalKCV.Models.ViewModel;

namespace JobPortalKCV.Controllers
{
    public class DocsController : Controller
    {
        public ActionResult Index(string lang = null, string q = null, string category = null)
        {
            var normalizedLang = NormalizeLang(lang);
            PersistLang(normalizedLang);

            var model = BuildModel(normalizedLang);
            model.Query = q;
            model.Category = category;
            model.AllSections = model.Sections;
            model.Sections = FilterSections(model.Sections, q, category);

            return View(model);
        }

        public ActionResult Detail(string id, string slug, string lang = null)
        {
            var normalizedLang = NormalizeLang(lang);
            PersistLang(normalizedLang);

            var topicSlug = !String.IsNullOrWhiteSpace(slug) ? slug : id;
            var docs = BuildModel(normalizedLang);
            var allTopics = docs.Sections.SelectMany(s => s.Items).ToList();
            var topic = allTopics.FirstOrDefault(i => String.Equals(i.Id, topicSlug, StringComparison.OrdinalIgnoreCase));

            if (topic == null)
                return HttpNotFound();

            var index = allTopics.IndexOf(topic);
            var section = docs.Sections.First(s => s.Items.Any(i => i.Id == topic.Id));

            return View(new DocsTopicViewModel
            {
                Docs = docs,
                Section = section,
                Topic = topic,
                BackToDocsText = normalizedLang == "vi" ? "Quay lại Docs" : "Back to Docs",
                OpenPageText = normalizedLang == "vi" ? "Mở trang liên quan" : "Open related page",
                PreviousText = normalizedLang == "vi" ? "Trước" : "Previous",
                NextText = normalizedLang == "vi" ? "Tiếp theo" : "Next",
                PreviousTopic = index > 0 ? allTopics[index - 1] : null,
                NextTopic = index < allTopics.Count - 1 ? allTopics[index + 1] : null
            });
        }

        public ActionResult Topic(string id, string lang = null)
        {
            return RedirectToAction("Detail", new { id = id, lang = NormalizeLang(lang) });
        }

        private string NormalizeLang(string lang)
        {
            if (IsSupportedLang(lang))
                return lang.ToLowerInvariant();

            var cookieLang = Request.Cookies["kcv_docs_lang"] == null ? null : Request.Cookies["kcv_docs_lang"].Value;
            return IsSupportedLang(cookieLang) ? cookieLang.ToLowerInvariant() : "en";
        }

        private bool IsSupportedLang(string lang)
        {
            return String.Equals(lang, "en", StringComparison.OrdinalIgnoreCase) ||
                   String.Equals(lang, "vi", StringComparison.OrdinalIgnoreCase);
        }

        private void PersistLang(string lang)
        {
            Response.Cookies.Add(new HttpCookie("kcv_docs_lang", lang)
            {
                Expires = DateTime.Now.AddYears(1),
                HttpOnly = false
            });
        }

        private List<DocsSectionViewModel> FilterSections(List<DocsSectionViewModel> sections, string query, string category)
        {
            var normalizedQuery = (query ?? "").Trim();
            var normalizedCategory = (category ?? "").Trim();

            return sections
                .Where(section => String.IsNullOrWhiteSpace(normalizedCategory) ||
                                  String.Equals(section.Id, normalizedCategory, StringComparison.OrdinalIgnoreCase))
                .Select(section => new DocsSectionViewModel
                {
                    Id = section.Id,
                    Title = section.Title,
                    Summary = section.Summary,
                    Items = section.Items
                        .Where(item => MatchesQuery(section, item, normalizedQuery))
                        .ToList()
                })
                .Where(section => section.Items.Any())
                .ToList();
        }

        private bool MatchesQuery(DocsSectionViewModel section, DocsItemViewModel item, string query)
        {
            if (String.IsNullOrWhiteSpace(query))
                return true;

            var haystack = String.Join(" ", new[]
            {
                section.Title,
                section.Summary,
                item.Title,
                item.Summary,
                item.Body
            });

            return haystack.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private DocsViewModel BuildModel(string lang)
        {
            return lang == "vi" ? BuildVietnameseModel() : BuildEnglishModel();
        }

        private DocsViewModel BuildEnglishModel()
        {
            return new DocsViewModel
            {
                Lang = "en",
                Title = "User Guide",
                Subtitle = "Role-based product documentation for KCV Job Portal.",
                IntroText = "Browse short guides by role, then open a dedicated guide page for step-by-step instructions, notes, and related pages.",
                SearchPlaceholder = "Search documentation",
                NoResultsText = "No results found.",
                TocTitle = "On this page",
                LanguageLabel = "Language",
                EnglishLabel = "English",
                VietnameseLabel = "Tiếng Việt",
                ReadGuideText = "Read guide",
                CategoryLabel = "Category",
                Sections = new List<DocsSectionViewModel>
                {
                    Section("getting-started", "Getting Started", "Start here if you are new to the portal.",
                        Doc("create-account", "Create an account", "Register as a candidate or employer and complete account verification.", "Register", "Account", "Register",
                            Content("Before you start",
                                new[] { "Choose the account type that matches your real workflow. Candidates use the portal to apply for jobs. Employers use it to manage companies, jobs, applications, and invitations." },
                                null,
                                new[] { "Use an email address you can access.", "Prepare company information if you register as an employer.", "Do not create duplicate accounts when verification is pending." },
                                "Employer registration can create a new company or request access to an existing company.",
                                null),
                            Content("Step-by-step",
                                null,
                                new[] { "Open Register.", "Select the correct registration type.", "Enter your account information.", "Submit the form and complete email verification when prompted.", "Sign in after verification succeeds." },
                                null,
                                null,
                                null)),
                        Doc("sign-in-verify-account", "Sign in and verify account", "Use email or username to sign in and handle OTP verification when required.", "Login", "Account", "Login",
                            Content("How sign in works",
                                new[] { "The login page accepts an email address or username with a password. If the account still needs verification, the system asks for OTP verification before normal access." },
                                null,
                                null,
                                "If your account is disabled, contact an administrator instead of registering again.",
                                null),
                            Content("Common checks",
                                null,
                                new[] { "Confirm the username or email is correct.", "Check that the password is typed correctly.", "Complete OTP verification if the system requires it.", "Use forgot password if you cannot recover the password." },
                                null,
                                null,
                                null)),
                        Doc("find-jobs", "Find jobs", "Search public jobs by keyword and filters, then open the job detail page.", "Index", "Jobs", "Find Jobs",
                            Content("Search options",
                                new[] { "Find Jobs lists available jobs and supports filters such as keyword, location, category, skill, and sort order." },
                                null,
                                new[] { "Start with a broad keyword.", "Add filters one by one.", "Open job detail before applying." },
                                null,
                                null),
                            Content("What to review",
                                null,
                                null,
                                new[] { "Job title and company.", "Salary range and employment type.", "Location and deadline.", "Apply action if you are signed in as a candidate." },
                                null,
                                null))),

                    Section("guest", "Guest", "Public pages available before signing in.",
                        Doc("browse-jobs", "Browse jobs", "Read public job listings before creating an account.", "Index", "Jobs", "Find Jobs",
                            Content("What guests can do", new[] { "Guests can view public jobs and open detail pages. Applying requires a candidate account." }, null, new[] { "Use filters to narrow the list.", "Review the company before applying.", "Register as a candidate when you are ready to apply." }, null, null)),
                        Doc("browse-companies", "Browse companies", "Explore public company profiles and open jobs.", "Index", "Companies", "Companies",
                            Content("Company pages", new[] { "Company profiles can show public contact information, website, logo, description, stars, and open jobs depending on company visibility settings." }, null, new[] { "Open a company detail page.", "Check open jobs under that company.", "Use Find Jobs for broader discovery." }, null, null)),
                        Doc("use-public-search", "Use public search", "Use the search flow to locate jobs or companies quickly.", "Results", "Search", "Search",
                            Content("Search behavior", new[] { "Search helps you discover public content from one place, then redirects you to the relevant job or company page." }, null, new[] { "Use simple keywords.", "Clear narrow filters if there are no results.", "Open the detail page to confirm the match." }, null, null))),

                    Section("candidate", "Candidate", "Manage profile, CVs, applications, invitations, and saved items.",
                        Doc("create-edit-profile", "Create and edit profile", "Maintain the candidate profile employers review.", "Me", "Profile", "My Profile",
                            Content("Profile purpose", new[] { "Your profile is the main candidate page employers inspect before inviting you or reviewing an application." }, null, new[] { "Personal information.", "Education and experience.", "Projects and skills.", "Avatar and about section." }, null, null),
                            Content("Recommended workflow", null, new[] { "Open My Profile.", "Choose Edit.", "Update each profile section.", "Save and reopen the public view to check the result." }, null, "Keep profile information concise and relevant to the jobs you want.", null)),
                        Doc("manage-cvs", "Manage CVs", "Upload CV files and choose the default CV for applications.", "Index", "UserCVs", "CV Management",
                            Content("Before uploading", new[] { "Use CV Management when you want the system to attach a prepared CV to job applications." }, null, new[] { "Use a supported file extension shown on the form.", "Keep the file size within the displayed limit.", "Name the file clearly so you can identify it later." }, null, "Do not rename a file extension manually to bypass validation."),
                            Content("Step-by-step", null, new[] { "Open CV Management from the user menu.", "Select a supported CV file.", "Upload the file.", "Set the best CV as default when you have more than one." }, null, null, null)),
                        Doc("apply-for-job", "Apply for a job", "Submit a CV and cover letter for a selected job.", "Index", "Jobs", "Find Jobs",
                            Content("Before you start", new[] { "Apply only after reviewing the job detail page. The system checks candidate access, CV availability, and duplicate applications." }, null, new[] { "Sign in as a candidate.", "Upload at least one valid CV.", "Review deadline and job details." }, null, null),
                            Content("Step-by-step", null, new[] { "Open Find Jobs.", "Open a job detail page.", "Select a CV if the form asks for one.", "Write a focused cover letter.", "Submit the application." }, null, "After applying, track the status in My Applications.", null)),
                        Doc("view-my-applications", "View my applications", "Track submitted applications and interview information.", "Index", "CandidateApplications", "My Applications",
                            Content("What you can see", new[] { "My Applications shows submitted applications, current status, interview details when available, and final result after the employer completes the process." }, null, new[] { "Pending applications are waiting for employer review.", "Interview status means an interview has been scheduled.", "Completed applications show final outcome." }, null, null)),
                        Doc("manage-invitations", "Manage invitations", "Accept or decline invitations from employers.", "Index", "CandidateInvitations", "Invitations",
                            Content("Invitation workflow", new[] { "Employers can invite candidates to apply for jobs. Candidates review each invitation before accepting or declining it." }, null, new[] { "Open Invitations.", "Read the job and company information.", "Accept if you want to continue.", "Decline if it is not relevant." }, null, null)),
                        Doc("saved-items", "Saved items", "Return to starred jobs, companies, or candidates.", "Saved", "Stars", "Saved Items",
                            Content("How saving works", new[] { "Use the star icon where it appears. Saved Items groups records you marked so you can return later without searching again." }, null, new[] { "Star a job or company from supported pages.", "Open Saved Items from the user menu.", "Unstar records you no longer need." }, null, null))),

                    Section("employer", "Employer", "Manage companies, jobs, applications, interviews, and invitations.",
                        Doc("manage-company-profile", "Manage company profile", "Update company information, logo, visibility, and ownership-related settings.", "Index", "CompanySettings", "Company Settings",
                            Content("Company workspace", new[] { "Company Settings lists companies connected to your employer account. Select a company to manage its public profile and operational settings." }, null, new[] { "Company name and industry.", "Logo and website.", "Contact information.", "Public profile and job visibility." }, null, null),
                            Content("Step-by-step", null, new[] { "Open Company Settings.", "Choose the company you manage.", "Edit profile fields.", "Save changes and review the public company page." }, null, null, null)),
                        Doc("post-manage-jobs", "Post and manage jobs", "Create, edit, or remove jobs for companies you manage.", "Index", "Jobs", "Manage Jobs",
                            Content("Job management", new[] { "Employers use Manage Jobs to create listings and maintain existing jobs. The job form uses company, location, employment type, salary, description, and deadline information." }, null, null, null, null),
                            Content("Create a job", null, new[] { "Open Manage Jobs.", "Choose Create.", "Select a company you manage.", "Enter job details and deadline.", "Save and confirm the job appears in the list." }, null, "If company logo is required, complete the company profile before posting.", null)),
                        Doc("review-applications", "Review applications", "Review candidate applications and decide the next action.", "Index", "Applications", "Applications",
                            Content("Application review", new[] { "Applications shows submissions for jobs under companies you manage." }, null, new[] { "Review candidate profile and CV.", "Read the cover letter.", "Reject unsuitable applications.", "Invite promising candidates to interview." }, null, null)),
                        Doc("schedule-interviews", "Schedule interviews", "Invite an applicant to interview from the applications workflow.", "Index", "Applications", "Applications",
                            Content("Interview flow", new[] { "When an application is pending, the employer can open the interview form and enter interview details. The system notifies the candidate." }, null, new[] { "Use a future interview date.", "Include contact information and location or online meeting details.", "Add useful notes for the candidate." }, "Interview invitation sends an in-app notification and email when mail delivery is configured.", null)),
                        Doc("sent-invitations", "Sent invitations", "Track candidate invitations sent by your employer account.", "Sent", "CandidateInvitations", "Sent Invitations",
                            Content("Invitation tracking", new[] { "Sent Invitations lists invitations you have sent to candidates and supports status filtering." }, null, new[] { "Pending means the candidate has not responded.", "Accepted means the candidate accepted the invitation.", "Declined means the candidate rejected it." }, null, null)),
                        Doc("saved-candidates", "Saved candidates", "Use saved/starred records to return to useful candidate profiles.", "Saved", "Stars", "Saved Candidates",
                            Content("Saved candidates", new[] { "Employers can use star-supported pages to save candidate-related records for later review." }, null, new[] { "Star useful records where the star action appears.", "Open Saved Candidates or Saved Items.", "Remove stars when records are no longer relevant." }, null, null))),

                    Section("admin", "Admin", "Use admin tools for system overview, users, records, and settings.",
                        Doc("admin-dashboard", "Use admin dashboard", "Review operational overview and admin shortcuts.", "Index", "Admin", "Admin Dashboard",
                            Content("Dashboard purpose", new[] { "The admin dashboard is the starting point for monitoring counts, charts, recent activity, and admin table shortcuts." }, null, new[] { "Use it to navigate to major admin areas.", "Review charts for quick context.", "Open tables for record-level work." }, null, null)),
                        Doc("manage-users", "Manage users", "Search, inspect, create, edit, enable, disable, or delete users where allowed.", "Table", "Admin", "Manage Users", new { table = "Users" },
                            Content("User administration", new[] { "The Users table supports search, details, create, edit, enable, disable, and delete actions according to admin rules." }, null, new[] { "Use disable for access control.", "Open details before editing unclear records.", "Avoid deleting records that still represent real history." }, null, null)),
                        Doc("manage-companies", "Manage companies", "Administer company records through the admin table.", "Table", "Admin", "Manage Companies", new { table = "Companies" },
                            Content("Company records", new[] { "Admins can use the Companies table for record-level management and the public Companies area to inspect the user-facing result." }, null, new[] { "Search by company fields.", "Open details before editing.", "Keep logo and visibility consistent with employer-facing settings." }, null, null)),
                        Doc("manage-jobs-admin", "Manage jobs", "Administer job records through the admin table.", "Table", "Admin", "Manage Jobs", new { table = "Jobs" },
                            Content("Job records", new[] { "Admins can inspect and maintain job records from the Jobs admin table." }, null, new[] { "Search for a job record.", "Open details to review long fields.", "Edit only the fields that need correction." }, null, null)),
                        Doc("system-settings", "System settings", "Configure site name, upload rules, pagination, maintenance, and related system settings.", "Index", "AdminSystemSettings", "System Settings",
                            Content("Settings scope", new[] { "System Settings is an admin-only area. It controls operational settings such as site name, logos, upload limits, allowed file types, OTP timing, pagination, maintenance mode, and company logo requirements." }, null, null, "Do not direct normal users to System Settings. They should follow the validation message shown on their form.", null))),

                    Section("faq", "FAQ", "Answers to common user questions.",
                        Doc("cannot-sign-in", "I cannot sign in", "Check credentials, verification, disabled status, and password recovery.", "Login", "Account", "Login",
                            Content("Troubleshooting", null, new[] { "Confirm username or email.", "Check password spelling.", "Complete OTP verification if prompted.", "Use forgot password when needed.", "Contact admin if the account is disabled." }, null, null, null)),
                        Doc("cannot-upload-cv", "I cannot upload my CV", "Check file type, file size, and validation messages on the upload form.", "Index", "UserCVs", "CV Management",
                            Content("Fix upload problems", null, new[] { "Read the validation message on the CV upload form.", "Use an allowed file extension.", "Reduce file size if needed.", "Export the CV again from your document editor." }, null, null, "Do not rename a file extension to bypass validation.")),
                        Doc("cannot-apply-job", "I cannot apply for a job", "Check role, CV availability, duplicate applications, and job availability.", "Index", "Jobs", "Find Jobs",
                            Content("Checklist", null, new[] { "Sign in as a candidate.", "Upload a usable CV.", "Check that the job is still visible.", "Confirm you have not already applied to the same job." }, null, null, null)),
                        Doc("wrong-menu", "I do not see the correct menu", "Menus depend on your role and sign-in state.", "Login", "Account", "Login",
                            Content("Why menus differ", new[] { "The navigation changes for guests, candidates, employers, and admins. If you see the wrong menu, your account may have the wrong role or you may not be signed in." }, null, new[] { "Sign out and sign in again.", "Confirm the account role.", "Contact admin if the role is wrong." }, null, null))),

                    Section("common-issues", "Common Issues", "Common errors and practical fixes.",
                        Doc("otp-expired", "OTP expired", "Request a new OTP and verify with the latest code.", "VerifyEmail", "Account", "Verify Email",
                            Content("How to recover", null, new[] { "Return to the verification page.", "Request a new OTP if available.", "Use the newest OTP sent to your email.", "Avoid trying old OTP codes." }, null, null, null)),
                        Doc("invalid-cv-file", "Invalid CV file", "Use a supported CV file format and size.", "Index", "UserCVs", "CV Management",
                            Content("What it means", new[] { "The CV upload was rejected because the file did not match the upload rules shown on the form." }, null, new[] { "Check the allowed extensions on the form.", "Check the file size.", "Export the document again as an accepted format." }, null, "Normal users do not need access to admin settings to fix this.")),
                        Doc("access-denied", "Access denied", "The page requires a role or ownership you do not currently have.", "Index", "Home", "Home",
                            Content("Access rules", new[] { "Some pages require a specific role. Employer actions also require access to the company that owns the job or invitation." }, null, new[] { "Confirm you are signed in.", "Check your role.", "For employer actions, confirm company membership or ownership.", "Ask admin for access if needed." }, null, null)),
                        Doc("no-records-found", "No records found", "Clear filters or broaden your search.", "Index", "Jobs", "Find Jobs",
                            Content("Search recovery", null, new[] { "Clear all filters.", "Use a broader keyword.", "Remove location, category, or skill filters one at a time.", "Try sorting by newest." }, null, null, null)))
                }
            };
        }

        private DocsViewModel BuildVietnameseModel()
        {
            return new DocsViewModel
            {
                Lang = "vi",
                Title = "Hướng dẫn sử dụng",
                Subtitle = "Tài liệu theo vai trò cho KCV Job Portal.",
                IntroText = "Xem danh sách hướng dẫn ngắn theo vai trò, sau đó mở từng trang chi tiết để đọc các bước thao tác, lưu ý và trang liên quan.",
                SearchPlaceholder = "Tìm kiếm tài liệu",
                NoResultsText = "Không tìm thấy nội dung.",
                TocTitle = "Trong trang này",
                LanguageLabel = "Ngôn ngữ",
                EnglishLabel = "English",
                VietnameseLabel = "Tiếng Việt",
                ReadGuideText = "Xem hướng dẫn",
                CategoryLabel = "Danh mục",
                Sections = new List<DocsSectionViewModel>
                {
                    Section("getting-started", "Bắt đầu", "Bắt đầu tại đây nếu bạn mới dùng hệ thống.",
                        Doc("create-account", "Tạo tài khoản", "Đăng ký ứng viên hoặc nhà tuyển dụng và hoàn tất xác thực.", "Register", "Account", "Đăng ký",
                            Content("Trước khi bắt đầu", new[] { "Chọn đúng loại tài khoản theo nhu cầu. Ứng viên dùng hệ thống để ứng tuyển. Nhà tuyển dụng dùng hệ thống để quản lý công ty, tin tuyển dụng, hồ sơ và lời mời." }, null, new[] { "Dùng email bạn có thể truy cập.", "Chuẩn bị thông tin công ty nếu đăng ký nhà tuyển dụng.", "Không tạo tài khoản trùng khi đang chờ xác thực." }, "Tài khoản employer có thể tạo công ty mới hoặc yêu cầu tham gia công ty đã tồn tại.", null),
                            Content("Các bước thực hiện", null, new[] { "Mở trang Đăng ký.", "Chọn loại đăng ký phù hợp.", "Nhập thông tin tài khoản.", "Gửi form và xác thực email khi được yêu cầu.", "Đăng nhập sau khi xác thực thành công." }, null, null, null)),
                        Doc("sign-in-verify-account", "Đăng nhập và xác thực tài khoản", "Đăng nhập bằng email hoặc username và xử lý OTP khi cần.", "Login", "Account", "Đăng nhập",
                            Content("Cách đăng nhập hoạt động", new[] { "Trang đăng nhập nhận email hoặc username cùng mật khẩu. Nếu tài khoản còn cần xác thực, hệ thống sẽ yêu cầu OTP trước khi vào đầy đủ chức năng." }, null, null, "Nếu tài khoản bị vô hiệu hóa, hãy liên hệ admin thay vì đăng ký lại.", null),
                            Content("Các kiểm tra thường dùng", null, new[] { "Kiểm tra đúng username hoặc email.", "Kiểm tra mật khẩu.", "Hoàn tất OTP nếu hệ thống yêu cầu.", "Dùng quên mật khẩu nếu không thể khôi phục mật khẩu." }, null, null, null)),
                        Doc("find-jobs", "Tìm việc", "Tìm job công khai bằng từ khóa và bộ lọc, sau đó mở trang chi tiết.", "Index", "Jobs", "Tìm việc",
                            Content("Tùy chọn tìm kiếm", new[] { "Trang Tìm việc hiển thị các job đang có và hỗ trợ lọc theo từ khóa, địa điểm, danh mục, kỹ năng và sắp xếp." }, null, new[] { "Bắt đầu bằng từ khóa rộng.", "Thêm từng bộ lọc một.", "Mở chi tiết job trước khi ứng tuyển." }, null, null),
                            Content("Nội dung nên kiểm tra", null, null, new[] { "Tiêu đề và công ty.", "Mức lương và loại việc.", "Địa điểm và hạn nộp.", "Nút ứng tuyển nếu bạn đã đăng nhập bằng tài khoản ứng viên." }, null, null))),

                    Section("guest", "Khách", "Các trang công khai trước khi đăng nhập.",
                        Doc("browse-jobs", "Xem việc làm", "Xem danh sách job công khai trước khi tạo tài khoản.", "Index", "Jobs", "Tìm việc",
                            Content("Khách có thể làm gì", new[] { "Khách có thể xem job công khai và mở trang chi tiết. Ứng tuyển yêu cầu tài khoản ứng viên." }, null, new[] { "Dùng bộ lọc để thu hẹp danh sách.", "Xem công ty trước khi ứng tuyển.", "Đăng ký ứng viên khi sẵn sàng nộp hồ sơ." }, null, null)),
                        Doc("browse-companies", "Xem công ty", "Xem hồ sơ công ty công khai và job đang mở.", "Index", "Companies", "Công ty",
                            Content("Trang công ty", new[] { "Hồ sơ công ty có thể hiển thị thông tin liên hệ công khai, website, logo, mô tả, lượt star và job đang mở tùy theo cấu hình hiển thị." }, null, new[] { "Mở trang chi tiết công ty.", "Kiểm tra job đang mở.", "Dùng Tìm việc để tìm rộng hơn." }, null, null)),
                        Doc("use-public-search", "Dùng tìm kiếm", "Tìm nhanh job hoặc công ty.", "Results", "Search", "Tìm kiếm",
                            Content("Cách tìm kiếm hoạt động", new[] { "Tìm kiếm giúp bạn phát hiện nội dung công khai từ một nơi, sau đó mở trang job hoặc công ty phù hợp." }, null, new[] { "Dùng từ khóa đơn giản.", "Xóa bộ lọc quá hẹp nếu không có kết quả.", "Mở trang chi tiết để xác nhận kết quả." }, null, null))),

                    Section("candidate", "Ứng viên", "Quản lý hồ sơ, CV, ứng tuyển, lời mời và mục đã lưu.",
                        Doc("create-edit-profile", "Tạo và sửa hồ sơ", "Cập nhật hồ sơ ứng viên để employer xem xét.", "Me", "Profile", "Hồ sơ của tôi",
                            Content("Mục đích hồ sơ", new[] { "Hồ sơ là trang chính để employer xem trước khi mời bạn hoặc xét hồ sơ ứng tuyển." }, null, new[] { "Thông tin cá nhân.", "Học vấn và kinh nghiệm.", "Dự án và kỹ năng.", "Avatar và phần giới thiệu." }, null, null),
                            Content("Luồng đề xuất", null, new[] { "Mở Hồ sơ của tôi.", "Chọn Edit.", "Cập nhật từng phần hồ sơ.", "Lưu và mở lại trang xem để kiểm tra." }, null, "Giữ thông tin ngắn gọn và liên quan tới công việc bạn muốn ứng tuyển.", null)),
                        Doc("manage-cvs", "Quản lý CV", "Upload CV và chọn CV mặc định để ứng tuyển.", "Index", "UserCVs", "Quản lý CV",
                            Content("Trước khi upload", new[] { "Dùng Quản lý CV khi bạn muốn hệ thống đính kèm CV chuẩn bị sẵn vào hồ sơ ứng tuyển." }, null, new[] { "Dùng đúng định dạng hiển thị trên form.", "Giữ dung lượng trong giới hạn cho phép.", "Đặt tên file rõ ràng để dễ nhận biết." }, null, "Không đổi đuôi file thủ công để vượt qua kiểm tra."),
                            Content("Các bước", null, new[] { "Mở Quản lý CV từ menu user.", "Chọn file CV được hỗ trợ.", "Upload file.", "Đặt CV phù hợp nhất làm mặc định nếu có nhiều CV." }, null, null, null)),
                        Doc("apply-for-job", "Ứng tuyển", "Gửi CV và cover letter cho job đã chọn.", "Index", "Jobs", "Tìm việc",
                            Content("Trước khi bắt đầu", new[] { "Chỉ ứng tuyển sau khi đọc kỹ trang chi tiết job. Hệ thống kiểm tra quyền ứng viên, CV và hồ sơ trùng." }, null, new[] { "Đăng nhập bằng tài khoản ứng viên.", "Upload ít nhất một CV hợp lệ.", "Kiểm tra hạn nộp và thông tin job." }, null, null),
                            Content("Các bước", null, new[] { "Mở Tìm việc.", "Mở trang chi tiết job.", "Chọn CV nếu form yêu cầu.", "Viết cover letter tập trung vào vị trí đó.", "Gửi hồ sơ ứng tuyển." }, null, "Sau khi ứng tuyển, theo dõi trạng thái ở Đã ứng tuyển.", null)),
                        Doc("view-my-applications", "Xem hồ sơ đã ứng tuyển", "Theo dõi hồ sơ đã nộp và thông tin phỏng vấn.", "Index", "CandidateApplications", "Đã ứng tuyển",
                            Content("Bạn có thể xem gì", new[] { "Đã ứng tuyển hiển thị các hồ sơ đã nộp, trạng thái hiện tại, thông tin phỏng vấn nếu có và kết quả cuối cùng khi employer hoàn tất xử lý." }, null, new[] { "Pending nghĩa là đang chờ employer xét.", "Interview nghĩa là đã có lịch phỏng vấn.", "Completed hiển thị kết quả cuối cùng." }, null, null)),
                        Doc("manage-invitations", "Quản lý lời mời", "Chấp nhận hoặc từ chối lời mời từ employer.", "Index", "CandidateInvitations", "Lời mời",
                            Content("Luồng lời mời", new[] { "Employer có thể mời ứng viên ứng tuyển vào job. Ứng viên nên xem từng lời mời trước khi chấp nhận hoặc từ chối." }, null, new[] { "Mở Lời mời.", "Đọc thông tin job và công ty.", "Chấp nhận nếu muốn tiếp tục.", "Từ chối nếu không phù hợp." }, null, null)),
                        Doc("saved-items", "Mục đã lưu", "Quay lại job, công ty hoặc ứng viên đã star.", "Saved", "Stars", "Đã lưu",
                            Content("Cách lưu hoạt động", new[] { "Dùng biểu tượng ngôi sao tại nơi được hỗ trợ. Trang Đã lưu nhóm các bản ghi bạn đã đánh dấu để quay lại mà không cần tìm lại." }, null, new[] { "Star job hoặc công ty ở trang hỗ trợ.", "Mở Đã lưu từ menu user.", "Unstar nội dung không còn cần." }, null, null))),

                    Section("employer", "Nhà tuyển dụng", "Quản lý công ty, job, hồ sơ, phỏng vấn và lời mời.",
                        Doc("manage-company-profile", "Quản lý hồ sơ công ty", "Cập nhật thông tin, logo, hiển thị và thiết lập công ty.", "Index", "CompanySettings", "Company Settings",
                            Content("Không gian công ty", new[] { "Company Settings hiển thị các công ty liên quan tới tài khoản employer. Chọn công ty để quản lý hồ sơ công khai và thiết lập vận hành." }, null, new[] { "Tên công ty và ngành.", "Logo và website.", "Thông tin liên hệ.", "Hiển thị profile và job." }, null, null),
                            Content("Các bước", null, new[] { "Mở Company Settings.", "Chọn công ty bạn quản lý.", "Sửa các trường hồ sơ.", "Lưu và kiểm tra trang công ty công khai." }, null, null, null)),
                        Doc("post-manage-jobs", "Đăng và quản lý job", "Tạo, sửa hoặc xóa job cho công ty bạn quản lý.", "Index", "Jobs", "Manage Jobs",
                            Content("Quản lý job", new[] { "Employer dùng Manage Jobs để tạo tin tuyển dụng và bảo trì job hiện có. Form job gồm công ty, địa điểm, loại việc, lương, mô tả và hạn nộp." }, null, null, null, null),
                            Content("Tạo job", null, new[] { "Mở Manage Jobs.", "Chọn Create.", "Chọn công ty bạn quản lý.", "Nhập thông tin job và hạn nộp.", "Lưu và kiểm tra job trong danh sách." }, null, "Nếu hệ thống yêu cầu logo công ty, hãy hoàn thiện hồ sơ công ty trước khi đăng.", null)),
                        Doc("review-applications", "Xem hồ sơ ứng tuyển", "Xem hồ sơ candidate và quyết định thao tác tiếp theo.", "Index", "Applications", "Applications",
                            Content("Xét hồ sơ", new[] { "Applications hiển thị hồ sơ nộp vào job thuộc công ty bạn quản lý." }, null, new[] { "Xem profile và CV của candidate.", "Đọc cover letter.", "Từ chối hồ sơ không phù hợp.", "Mời phỏng vấn ứng viên tiềm năng." }, null, null)),
                        Doc("schedule-interviews", "Lên lịch phỏng vấn", "Mời applicant phỏng vấn từ luồng Applications.", "Index", "Applications", "Applications",
                            Content("Luồng phỏng vấn", new[] { "Khi hồ sơ đang pending, employer có thể mở form interview và nhập thông tin phỏng vấn. Hệ thống sẽ thông báo cho candidate." }, null, new[] { "Dùng thời gian phỏng vấn trong tương lai.", "Nhập địa điểm hoặc link phỏng vấn online.", "Thêm thông tin liên hệ và ghi chú cần thiết." }, "Lời mời phỏng vấn có thể gửi thông báo trong hệ thống và email khi cấu hình mail hoạt động.", null)),
                        Doc("sent-invitations", "Lời mời đã gửi", "Theo dõi lời mời candidate do tài khoản employer gửi.", "Sent", "CandidateInvitations", "Sent Invitations",
                            Content("Theo dõi lời mời", new[] { "Sent Invitations hiển thị lời mời đã gửi tới candidate và hỗ trợ lọc theo trạng thái." }, null, new[] { "Pending nghĩa là candidate chưa phản hồi.", "Accepted nghĩa là candidate đã chấp nhận.", "Declined nghĩa là candidate đã từ chối." }, null, null)),
                        Doc("saved-candidates", "Ứng viên đã lưu", "Quay lại các hồ sơ candidate hữu ích.", "Saved", "Stars", "Saved Candidates",
                            Content("Ứng viên đã lưu", new[] { "Employer có thể dùng tính năng star ở các trang hỗ trợ để lưu bản ghi liên quan tới candidate." }, null, new[] { "Star các bản ghi hữu ích.", "Mở Saved Candidates hoặc Saved Items.", "Bỏ star khi không còn cần." }, null, null))),

                    Section("admin", "Admin", "Công cụ tổng quan, user, bản ghi và cấu hình hệ thống.",
                        Doc("admin-dashboard", "Dùng admin dashboard", "Xem tổng quan vận hành và lối tắt admin.", "Index", "Admin", "Admin Dashboard",
                            Content("Mục đích dashboard", new[] { "Admin dashboard là điểm bắt đầu để xem số lượng, biểu đồ, hoạt động gần đây và lối tắt tới các bảng quản trị." }, null, new[] { "Dùng để đi tới các khu vực admin chính.", "Xem biểu đồ để nắm tình hình nhanh.", "Mở bảng để xử lý ở mức bản ghi." }, null, null)),
                        Doc("manage-users", "Quản lý người dùng", "Tìm, xem, tạo, sửa, bật, tắt hoặc xóa user khi được phép.", "Table", "Admin", "Quản lý người dùng", new { table = "Users" },
                            Content("Quản trị user", new[] { "Bảng Users hỗ trợ tìm kiếm, xem chi tiết, tạo, sửa, bật, tắt và xóa theo quy tắc admin." }, null, new[] { "Dùng disable để kiểm soát truy cập.", "Mở details trước khi sửa bản ghi chưa rõ.", "Tránh xóa bản ghi còn đại diện cho lịch sử thật." }, null, null)),
                        Doc("manage-companies", "Quản lý công ty", "Quản trị bản ghi công ty qua bảng admin.", "Table", "Admin", "Quản lý công ty", new { table = "Companies" },
                            Content("Bản ghi công ty", new[] { "Admin có thể dùng bảng Companies để quản lý bản ghi và dùng trang Companies công khai để kiểm tra kết quả phía người dùng." }, null, new[] { "Tìm theo trường công ty.", "Mở details trước khi sửa.", "Giữ logo và hiển thị đồng bộ với phần employer." }, null, null)),
                        Doc("manage-jobs-admin", "Quản lý job", "Quản trị bản ghi job qua bảng admin.", "Table", "Admin", "Quản lý job", new { table = "Jobs" },
                            Content("Bản ghi job", new[] { "Admin có thể xem và bảo trì bản ghi job từ bảng Jobs trong admin." }, null, new[] { "Tìm bản ghi job.", "Mở details để xem các trường dài.", "Chỉ sửa các trường cần điều chỉnh." }, null, null)),
                        Doc("system-settings", "Cấu hình hệ thống", "Cấu hình tên site, upload, phân trang, bảo trì và các thiết lập liên quan.", "Index", "AdminSystemSettings", "System Settings",
                            Content("Phạm vi thiết lập", new[] { "System Settings là khu vực chỉ dành cho admin. Trang này kiểm soát tên site, logo, giới hạn upload, định dạng file, OTP, phân trang, maintenance mode và yêu cầu logo công ty." }, null, null, "Không hướng người dùng thường vào System Settings. Họ chỉ cần làm theo thông báo validation trên form.", null))),

                    Section("faq", "Câu hỏi thường gặp", "Giải đáp các câu hỏi phổ biến.",
                        Doc("cannot-sign-in", "Tôi không đăng nhập được", "Kiểm tra thông tin đăng nhập, xác thực, trạng thái disabled và khôi phục mật khẩu.", "Login", "Account", "Đăng nhập",
                            Content("Cách kiểm tra", null, new[] { "Kiểm tra username hoặc email.", "Kiểm tra mật khẩu.", "Hoàn tất OTP nếu được yêu cầu.", "Dùng quên mật khẩu khi cần.", "Liên hệ admin nếu tài khoản bị disable." }, null, null, null)),
                        Doc("cannot-upload-cv", "Tôi không upload được CV", "Kiểm tra định dạng, dung lượng và thông báo validation trên form upload.", "Index", "UserCVs", "Quản lý CV",
                            Content("Sửa lỗi upload", null, new[] { "Đọc thông báo lỗi trên form upload CV.", "Dùng đuôi file được cho phép.", "Giảm dung lượng nếu cần.", "Xuất lại CV từ trình soạn thảo tài liệu." }, null, null, "Không đổi đuôi file để vượt qua kiểm tra.")),
                        Doc("cannot-apply-job", "Tôi không ứng tuyển được", "Kiểm tra role, CV, hồ sơ trùng và trạng thái job.", "Index", "Jobs", "Tìm việc",
                            Content("Checklist", null, new[] { "Đăng nhập bằng tài khoản ứng viên.", "Upload CV hợp lệ.", "Kiểm tra job còn hiển thị.", "Đảm bảo bạn chưa ứng tuyển job này." }, null, null, null)),
                        Doc("wrong-menu", "Tôi không thấy đúng menu", "Menu phụ thuộc role và trạng thái đăng nhập.", "Login", "Account", "Đăng nhập",
                            Content("Vì sao menu khác nhau", new[] { "Navigation thay đổi theo guest, candidate, employer và admin. Nếu menu sai, tài khoản có thể sai role hoặc bạn chưa đăng nhập." }, null, new[] { "Đăng xuất và đăng nhập lại.", "Kiểm tra role tài khoản.", "Liên hệ admin nếu role bị sai." }, null, null))),

                    Section("common-issues", "Lỗi thường gặp", "Các lỗi phổ biến và cách xử lý.",
                        Doc("otp-expired", "OTP hết hạn", "Yêu cầu OTP mới và xác thực bằng mã mới nhất.", "VerifyEmail", "Account", "Xác thực email",
                            Content("Cách xử lý", null, new[] { "Quay lại trang xác thực.", "Yêu cầu OTP mới nếu có.", "Dùng OTP mới nhất trong email.", "Không thử lại các OTP cũ." }, null, null, null)),
                        Doc("invalid-cv-file", "File CV không hợp lệ", "Dùng định dạng và dung lượng CV được hỗ trợ.", "Index", "UserCVs", "Quản lý CV",
                            Content("Ý nghĩa lỗi", new[] { "CV bị từ chối vì file không khớp quy tắc upload hiển thị trên form." }, null, new[] { "Kiểm tra đuôi file được phép trên form.", "Kiểm tra dung lượng file.", "Xuất lại tài liệu sang định dạng được chấp nhận." }, null, "Người dùng thường không cần vào admin settings để xử lý lỗi này.")),
                        Doc("access-denied", "Bị từ chối truy cập", "Trang yêu cầu role hoặc quyền sở hữu mà bạn chưa có.", "Index", "Home", "Home",
                            Content("Quy tắc truy cập", new[] { "Một số trang yêu cầu role cụ thể. Thao tác employer còn yêu cầu quyền với công ty sở hữu job hoặc lời mời." }, null, new[] { "Đảm bảo đã đăng nhập.", "Kiểm tra role.", "Với employer, kiểm tra quyền công ty.", "Hỏi admin nếu cần cấp quyền." }, null, null)),
                        Doc("no-records-found", "Không có dữ liệu", "Xóa bộ lọc hoặc tìm rộng hơn.", "Index", "Jobs", "Tìm việc",
                            Content("Khôi phục kết quả", null, new[] { "Xóa toàn bộ bộ lọc.", "Dùng từ khóa rộng hơn.", "Bỏ lọc địa điểm, danh mục hoặc kỹ năng từng bước.", "Thử sắp xếp theo mới nhất." }, null, null, null)))
                }
            };
        }

        private DocsSectionViewModel Section(string id, string title, string summary, params DocsItemViewModel[] items)
        {
            var section = new DocsSectionViewModel
            {
                Id = id,
                Title = title,
                Summary = summary,
                Items = new List<DocsItemViewModel>(items)
            };

            foreach (var item in section.Items)
            {
                item.SectionId = id;
                item.SectionTitle = title;
            }

            return section;
        }

        private DocsItemViewModel Doc(string id, string title, string summary, string action, string controller, string linkText, params DocsContentSectionViewModel[] sections)
        {
            return Doc(id, title, summary, action, controller, linkText, null, sections);
        }

        private DocsItemViewModel Doc(string id, string title, string summary, string action, string controller, string linkText, object routeValues, params DocsContentSectionViewModel[] sections)
        {
            return new DocsItemViewModel
            {
                Id = id,
                Title = title,
                Summary = summary,
                Body = String.Join(" ", sections.SelectMany(section => new[] { section.Heading }
                    .Concat(section.Paragraphs ?? new List<string>())
                    .Concat(section.Steps ?? new List<string>())
                    .Concat(section.Bullets ?? new List<string>())
                    .Concat(new[] { section.Note, section.Warning }).Where(value => !String.IsNullOrWhiteSpace(value)))),
                Action = action,
                Controller = controller,
                RouteValues = routeValues,
                LinkText = linkText,
                ContentSections = new List<DocsContentSectionViewModel>(sections)
            };
        }

        private DocsContentSectionViewModel Content(string heading, IEnumerable<string> paragraphs, IEnumerable<string> steps, IEnumerable<string> bullets, string note, string warning)
        {
            return new DocsContentSectionViewModel
            {
                Heading = heading,
                Paragraphs = paragraphs == null ? new List<string>() : paragraphs.ToList(),
                Steps = steps == null ? new List<string>() : steps.ToList(),
                Bullets = bullets == null ? new List<string>() : bullets.ToList(),
                Note = note,
                Warning = warning
            };
        }
    }
}
