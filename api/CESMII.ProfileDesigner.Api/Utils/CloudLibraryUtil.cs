using System;
using System.Threading.Tasks;
using System.Net.Mail;

using Microsoft.Extensions.Logging;
using SendGrid.Helpers.Mail;

using CESMII.ProfileDesigner.DAL.Models;
using CESMII.ProfileDesigner.Common.Enums;
using CESMII.Common.SelfServiceSignUp.Services;
using System.Collections.Generic;

namespace CESMII.ProfileDesigner.Api.Utils
{

    public class CloudLibraryUtil
    {
        private readonly MailRelayService _mailService;
        protected readonly ILogger<CloudLibraryUtil> _logger;

        public CloudLibraryUtil()
        {
        }

        public CloudLibraryUtil(MailRelayService mailservice, ILogger<CloudLibraryUtil> logger)
        {
            _mailService = mailservice;
            _logger = logger;
        }

        public static string GetApprovalStatusString(ProfileStateEnum val)
        {
            switch (val)
            {
                case ProfileStateEnum.CloudLibPublished: return "PUBLISHED";
                case ProfileStateEnum.CloudLibPending: return "PENDING";
                case ProfileStateEnum.CloudLibApproved: return "APPROVED";
                case ProfileStateEnum.CloudLibRejected: return "REJECTED";
                case ProfileStateEnum.CloudLibCancelled: return "CANCELED";
                case ProfileStateEnum.Local: 
                case ProfileStateEnum.Core: 
                    return "";
                default: return "UNKNOWN";
            }
        }

        /// <summary>
        /// Email a notification for publish
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="user"></param>
        public async Task EmailPublishNotification(ProfileModel profile, UserModel user)
        {
            // Send email to notify recipient that we have received the cancel publish request
            try
            {
                var emailInfo = new EmailDataModel(profile, user,
                    "CESMII Cloud Library - Profile Submission");

                string body =
                    $"<p>Thank you for submitting your profile {profile.Namespace} to the Clean Energy Smart Manufacturing Innovation Institute (CESMII) Cloud Library. " +
                    $"Your submission will be reviewed by the CESMII team. " +
                    $"After approval, your profile will appear in the CESMII Cloud Library and Marketplace. </p>" +
                    $"<p></p>" +
                    $"<p>To check on the status of your submission, visit the Profile Library in the Profile Designer. " +
                    $"You can cancel your submission at any time from by clicking the <b>Cancel Publish</b> button in the Profile Library.</p>" +
                    $"<p></p>" +
                    $"<p>Please note that once a profile has been submitted for publishing, the profile is no longer editable.</p>" +
                    $"<p></p>" +
                    $"<p>Thank you for your support of the CESMII Profile Designer and again, thank you for your profile submission!</p>" +
                    $"<p></p>" +
                    $"<p>Sincerely,</p>" +
                    $"<p>CESMII Support Team</p>" +
                    $"<p></p>" +
                    $"<p></p>" +
                    $"<p></p>" +
                    $"<p style=\"margin-left: 50px;\">{emailInfo.AuthorInfo}</br>" +
                    $"{emailInfo.OrganizationInfo}</p>" +
                    $"<p style=\"margin-left: 50px;\">{emailInfo.ProfileInfo}</br>" +
                    $"</p>" +
                    $"<p></p>";
                /*
                string body = $"<p>Thank you very much for your submission to the Clean Energy Smart Manufacturing Innovation Institute (CESMII) Cloud Library.</p>" +
                                    $"<p></p>" +
                                    $"<p>{emailInfo.AuthorInfo}</p>" +
                                    $"<p>{emailInfo.OrganizationInfo}</p>" +
                                    $"<p></p>" +
                                    $"<p>{emailInfo.ProfileInfo}</p>" +
                                    $"<p></p>" +
                                    $"<p></p>" +
                                    $"<p>Sincerely,</p>" +
                                    $"<p>CESMII Support Team</p>" +
                                    $"<p></p>";
                 */

                await SendEmailNotification(emailInfo, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email notification for cancellation request {profile.ID} for user {user.ID}");
            }
        }

        /// <summary>
        /// Email a notification for a cancel publish
        /// </summary>
        /// <param name="profile"></param>
        /// <param name="user"></param>
        public async Task EmailCancelNotification(ProfileModel profile, UserModel user)
        {
            // Send email to notify recipient that we have received the cancel publish request
            try
            {
                var emailInfo = new EmailDataModel(profile, user,
                    "CESMII Cloud Library - Profile Submission Cancellation");

                string body =
                        $"<p>We have received your request to cancel the profile submission for profile {profile.Namespace}. " +
                        $"<p></p>" +
                        $"<p>Your may now edit your profile and submit it at a later time. " +
                        $"<p></p>" +
                        $"<p>Thank you again for your support of the CESMII Profile Designer!</p>" +
                        $"<p></p>" +
                        $"<p>Sincerely,</p>" +
                        $"<p>CESMII Support Team</p>" +
                        $"<p></p>" +
                        $"<p></p>" +
                        $"<p></p>" +
                        $"<p style=\"margin-left: 50px;\">{emailInfo.AuthorInfo}</br>" +
                        $"{emailInfo.OrganizationInfo}</p>" +
                        $"<p style=\"margin-left: 50px;\">{emailInfo.ProfileInfo}" +
                        $"</p>" +
                        $"<p></p>";

                await SendEmailNotification(emailInfo, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send email notification for cancellation request {profile.ID} for user {user.ID}");
            }
        }

        #region Email notification helpers

        /// <summary>
        /// Todo: public for now until we convert over ProfileController code to use the private method.
        /// </summary>
        public async Task SendEmailNotification(EmailDataModel emailInfo, string body)
        {
            // Setup "To" list 
            // List of recipients for the notification email.
            List<EmailAddress> leaTo = new List<EmailAddress>();
            if (emailInfo.AuthorEmail.ToLower() != emailInfo.SenderEmail.ToLower())
                leaTo.Add(new EmailAddress(emailInfo.AuthorEmail, emailInfo.AuthorDisplayName));
            leaTo.Add(new EmailAddress(emailInfo.SenderEmail, emailInfo.SenderDisplayName));

            // Setup Contents of our email message.
            MailMessage mm = new MailMessage()
            {
                Subject = emailInfo.Subject,
                Body = body
            };

            await _mailService.SendEmailSendGrid(mm, leaTo);
        }
        #endregion

    }

    public class EmailDataModel
    {
        private readonly string _senderEmail;
        private readonly string _senderDisplayName;
        private readonly string _authorEmail;
        private readonly string _authorDisplayName;
        private readonly string _authorInfo;
        private readonly string _organizationInfo;
        private readonly string _profileInfo;
        private readonly string _subject;

        public EmailDataModel(ProfileModel profile, UserModel user, string subject) {
            _senderEmail = user.Email;
            _senderDisplayName = user.DisplayName;
            _authorEmail = profile.Author.Email;
            _authorDisplayName = profile.Author.DisplayName;
            _authorInfo = $"\tAuthor: <strong>{profile.Author.DisplayName} [{profile.Author.Email}]</strong>";
            _organizationInfo = $"\tOrganization: <strong>{profile.Author.Organization}</strong>";
            _profileInfo = $"\tProfile Title: <strong>{profile.Title}</strong>: <br/>" +
                            $"\tProfile Description: <strong>{profile.Description}</strong> <br/>" +
                            $"\tProfile Namespace: <strong>{profile.Namespace}</strong>: <br/>" +
                            $"\tProfile Version: <strong>{profile.Version}</strong>: <br/>" +
                            $"\tProfile Publication Date: <strong>{profile.PublishDate}</strong> <br/>" +
                            $"\tProfile License: <strong>{profile.License}</strong> <br/>";
            _subject = subject;
        }

        public string SenderEmail { get { return _senderEmail; } }
        public string SenderDisplayName { get { return _senderDisplayName; } }
        public string AuthorEmail { get { return _authorEmail; } }
        public string AuthorDisplayName { get { return _authorDisplayName; } }
        public string AuthorInfo { get { return _authorInfo; } }
        public string OrganizationInfo { get { return _organizationInfo; } }
        public string ProfileInfo { get { return _profileInfo; } }
        public string Subject { get { return _subject; } }
    }
}
