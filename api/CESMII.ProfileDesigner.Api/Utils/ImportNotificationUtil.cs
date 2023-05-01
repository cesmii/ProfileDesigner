using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Mail;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

using SendGrid.Helpers.Mail;

using CESMII.Common.SelfServiceSignUp.Services;
using CESMII.ProfileDesigner.Api.Shared.Models;
using CESMII.ProfileDesigner.DAL.Models;
using CESMII.ProfileDesigner.Api.Shared.Utils;
using CESMII.ProfileDesigner.Common;

namespace CESMII.ProfileDesigner.Api.Utils
{
    /// <summary>
    /// Small class to facilitate notifying user upon completion of import. Separate it out to make DI easier
    /// </summary>
    public class ImportNotificationUtil
    {
        private readonly ConfigUtil _configUtil;
        private readonly MailRelayService _mailService;
        private readonly ICustomRazorViewEngine _viewEngine;
        private readonly ILogger<ImportNotificationUtil> _logger;

        private const string SUBJECT = "CESMII | Profile Designer | Nodeset Import {{STATUS}}";

        public ImportNotificationUtil(ConfigUtil configUtil,
            MailRelayService mailservice,
            ICustomRazorViewEngine viewEngine,
            ILogger<ImportNotificationUtil> logger)
        {
            _configUtil = configUtil;
            _mailService = mailservice;
            _viewEngine = viewEngine;
            _logger = logger;
        }

        public async Task SendEmailNotification(ImportLogModel logItem, UserModel user)
        {
            // Send email to notify importer user that import has completed, failed, cancelled
            try
            {
                var item = new ImportCompleteNotifyModel()
                {
                    ImportItem = logItem,
                    Author = user,
                    BaseUrl = _configUtil.MailSettings.BaseUrl
                };

                var subject = SUBJECT.Replace("{{STATUS}}", logItem.StatusName);
                //var emailInfo = new EmailDataModel(user, subject);
                string viewName = "~/Views/Template/EmailImportCompleteNotification.cshtml";

                // Setup "To" list 
                // List of recipients for the notification email.
                List<EmailAddress> leaTo = new List<EmailAddress>() { new EmailAddress() { Email = user.Email, Name = user.DisplayName } };

                // Setup Contents of our email message.
                MailMessage mm = new MailMessage()
                {
                    Subject = subject,
                    Body = await _viewEngine.RazorViewToHtmlAsync(viewName, item)
                };

                //sending email to the user who experienced the issue
                await _mailService.SendEmailSendGrid(mm, leaTo);

                
                //If import failed, send separate notification with some additional info to admin
                //Let devops at cesmii know if a failed import occurs
                if (logItem.Status == Common.Enums.TaskStatusEnum.Failed 
                    && _configUtil.MailSettings.NotifyImportFailureAddresses != null)
                {
                    //add admin user info to inform the template to include some extra info
                    //generate a new body for the email
                    item.AdminUserInfo = user;
                    mm.Subject += "|| Admin Notification";
                    mm.Body = await _viewEngine.RazorViewToHtmlAsync(viewName, item);
                    foreach (var addr in _configUtil.MailSettings.NotifyImportFailureAddresses)
                    {
                        leaTo.Add(new EmailAddress() { Email = addr });
                    }
                    await _mailService.SendEmailSendGrid(mm, leaTo);
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email notification for import complete {logItem.ID} for user {user.ID}");
            }

        }
    }
}
