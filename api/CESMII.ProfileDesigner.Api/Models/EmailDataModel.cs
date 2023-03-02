using CESMII.ProfileDesigner.DAL.Models;

namespace CESMII.ProfileDesigner.Api.Models
{
    public class EmailDataModel
    {
        private readonly string _senderEmail;
        private readonly string _senderDisplayName;
        private readonly string _authorEmail;
        private readonly string _authorDisplayName;
        private readonly string _subject;

        public EmailDataModel(ProfileModel profile, UserModel user, string subject)
        {
            _senderEmail = user.Email;
            _senderDisplayName = user.DisplayName;
            _authorEmail = profile.Author.Email;
            _authorDisplayName = profile.Author.DisplayName;
            _subject = subject;
        }

        public string SenderEmail { get { return _senderEmail; } }
        public string SenderDisplayName { get { return _senderDisplayName; } }
        public string AuthorEmail { get { return _authorEmail; } }
        public string AuthorDisplayName { get { return _authorDisplayName; } }
        public string Subject { get { return _subject; } }
    }
}
