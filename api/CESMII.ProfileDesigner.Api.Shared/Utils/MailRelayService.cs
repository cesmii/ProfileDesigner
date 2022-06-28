namespace CESMII.ProfileDesigner.Api.Shared.Utils
{
    using System;
    using System.Net;
    using System.Net.Mail;
    using System.Net.Security;
    using CESMII.ProfileDesigner.Common;
    using CESMII.ProfileDesigner.Common.Models;

    public class MailRelayService
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
        
        private readonly MailConfig _config;

        public MailRelayService(ConfigUtil configUtil)
        {
            _config = configUtil.MailSettings;
        }

        public bool SendEmail(MailMessage message)
        {
            // Do not proceed further if mail relay is disabled.
            if (!_config.Enabled)
            {
                Logger.Warn("Mail Relay is disabled.");
                return true;
            }

            // Configure the SMTP client and send the message
            var client = new SmtpClient
             {
                 Host = _config.Address,
                 Port = _config.Port,

                 // Use whatever SSL mode is set.
                 EnableSsl = _config.EnableSsl,
                 DeliveryMethod = SmtpDeliveryMethod.Network
            };

            Logger.Debug($"Email configuration | Server: {_config.Address} Port: {_config.Port} SSL: {_config.EnableSsl}");

            message.From = new MailAddress(_config.MailFromAddress, "Profile Designer Portal");

            // If Mail Relay is in debug mode set all addresses to the configuration file.
            if (_config.Debug)
            {
                Logger.Debug($"Mail relay is in debug mode. Redirecting target email to: {string.Join("|", _config.DebugToAddresses)}");
                message.To.Clear();
                foreach (var address in _config.DebugToAddresses)
                {
                    message.To.Add(new MailAddress(address));
                }
            }

            // If the user has setup credentials, use them.
            if (!string.IsNullOrEmpty(_config.Username) && !string.IsNullOrEmpty(_config.Password))
            {
                client.Credentials = new NetworkCredential(_config.Username, _config.Password);
                Logger.Debug("Credentials are set in app settings, will leverage for SMTP connection.");
            }

            try
            {
                client.Send(message);
            }
            catch (Exception ex)
            {
                if (ex is SmtpException)
                {
                    Logger.Error("An SMTP exception occurred while attempting to relay mail.");
                }
                else
                {
                    Logger.Error("A general exception occured while attempting to relay mail.");
                }

                Logger.Error(ex.Message);
                return false;
            }
            finally
            {
                message.Dispose();
            }

            Logger.Info("Message relay complete.");
            return true;
        }
    }
}