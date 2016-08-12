using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PipelineAuditToolkit.Providers;
using PipelineAuditToolkit.Utility;

namespace PipelineAuditToolkit.Tests
{
    [TestClass]
    public class UsernameTransformerTests
    {
        private const string DomainAppSetting = "UserNameTransformer.Translations";
        private const string UserRegexAppSetting = "UserNameTransformer.RegexUserTranslations";
        private const string UserFixedAppSetting = "UserNameTransformer.FixedUserTranslations";

        [TestMethod]
        public void Initialize_WhenNoDataExists_CompletesWithoutError()
        {
            var appSettings = new Mock<IConfigurationSettings>(MockBehavior.Strict);
            appSettings.Setup(a => a.GetApplicationSetting(DomainAppSetting, null)).Returns((string)null);
            appSettings.Setup(a => a.GetApplicationSetting(UserRegexAppSetting, null)).Returns((string)null);
            appSettings.Setup(a => a.GetApplicationSetting(UserFixedAppSetting, null)).Returns((string)null);

            var logger = new Mock<ILogger>();

            var transformer = new UsernameTransformer(appSettings.Object, logger.Object);

            transformer.Initalize();

            Assert.AreEqual(0, transformer.DomainRuleCount);
            
            appSettings.VerifyAll();
        }

        [TestMethod]
        public void Initialize_WhenInvalidDataExists_CompletesWithoutErrorButIgnoresRule()
        {
            var appSettings = new Mock<IConfigurationSettings>(MockBehavior.Strict);
            appSettings.Setup(a => a.GetApplicationSetting(DomainAppSetting, null)).Returns("mycompany\\.com");
            appSettings.Setup(a => a.GetApplicationSetting(UserRegexAppSetting, null)).Returns((string)null);
            appSettings.Setup(a => a.GetApplicationSetting(UserFixedAppSetting, null)).Returns((string)null);


            var logger = new Mock<ILogger>();

            var transformer = new UsernameTransformer(appSettings.Object, logger.Object);

            transformer.Initalize();

            Assert.AreEqual(0, transformer.DomainRuleCount);

            appSettings.VerifyAll();
        }

        [TestMethod]
        public void Initialize_WhenValidDataExists_CompletesWithoutError()
        {
            var appSettings = new Mock<IConfigurationSettings>(MockBehavior.Strict);
            appSettings.Setup(a => a.GetApplicationSetting(DomainAppSetting, null)).Returns("mycompany\\.com:othercompany.com");
            appSettings.Setup(a => a.GetApplicationSetting(UserRegexAppSetting, null)).Returns((string)null);
            appSettings.Setup(a => a.GetApplicationSetting(UserFixedAppSetting, null)).Returns((string)null);


            var logger = new Mock<ILogger>();

            var transformer = new UsernameTransformer(appSettings.Object, logger.Object);

            transformer.Initalize();

            Assert.AreEqual(1, transformer.DomainRuleCount);

            appSettings.VerifyAll();
        }

        [TestMethod]
        public void Initialize_WhenValidUserRegexExists_CompletesWithoutError()
        {
            var appSettings = new Mock<IConfigurationSettings>(MockBehavior.Strict);
            appSettings.Setup(a => a.GetApplicationSetting(DomainAppSetting, null)).Returns((string)null);
            appSettings.Setup(a => a.GetApplicationSetting(UserRegexAppSetting, null)).Returns("([A-Za-z]).+\\.([A-Za-z]+):$1$0");
            appSettings.Setup(a => a.GetApplicationSetting(UserFixedAppSetting, null)).Returns((string)null);


            var logger = new Mock<ILogger>();

            var transformer = new UsernameTransformer(appSettings.Object, logger.Object);

            transformer.Initalize();

            Assert.AreEqual(1, transformer.UserRegexRuleCount);

            appSettings.VerifyAll();
        }

        public void Initialize_WhenValidUserFixedExists_CompletesWithoutError()
        {
            var appSettings = new Mock<IConfigurationSettings>(MockBehavior.Strict);
            appSettings.Setup(a => a.GetApplicationSetting(DomainAppSetting, null)).Returns((string)null);
            appSettings.Setup(a => a.GetApplicationSetting(UserRegexAppSetting, null)).Returns((string)null);
            appSettings.Setup(a => a.GetApplicationSetting(UserFixedAppSetting, null)).Returns("johndoe:jdoe");


            var logger = new Mock<ILogger>();

            var transformer = new UsernameTransformer(appSettings.Object, logger.Object);

            transformer.Initalize();

            Assert.AreEqual(1, transformer.UserFixedRuleCount);

            appSettings.VerifyAll();
        }

        [TestMethod]
        public void Initialize_WhenRegexIsMalformed_CompletesWithoutErrorButIgnoresRule()
        {
            var appSettings = new Mock<IConfigurationSettings>(MockBehavior.Strict);
            appSettings.Setup(a => a.GetApplicationSetting(DomainAppSetting, null)).Returns("mycompany[\\.com:othercompany.com");
            appSettings.Setup(a => a.GetApplicationSetting(UserRegexAppSetting, null)).Returns((string)null);
            appSettings.Setup(a => a.GetApplicationSetting(UserFixedAppSetting, null)).Returns((string)null);


            var logger = new Mock<ILogger>();

            var transformer = new UsernameTransformer(appSettings.Object, logger.Object);

            transformer.Initalize();

            Assert.AreEqual(0, transformer.DomainRuleCount);

            appSettings.VerifyAll();
        }

        [TestMethod]
        public void GetEmailAddress_WhenOriginalStringIsNotAnEmail_ReturnsValueInLowerCase()
        {
            var appSettings = new Mock<IConfigurationSettings>(MockBehavior.Strict);
            appSettings.Setup(a => a.GetApplicationSetting(DomainAppSetting, null)).Returns("mycompany\\.com:othercompany.com");
            appSettings.Setup(a => a.GetApplicationSetting(UserRegexAppSetting, null)).Returns((string)null);
            appSettings.Setup(a => a.GetApplicationSetting(UserFixedAppSetting, null)).Returns((string)null);


            var logger = new Mock<ILogger>();

            var transformer = new UsernameTransformer(appSettings.Object, logger.Object);

            transformer.Initalize();

            var result = transformer.GetEmailAddress("FOO");

            Assert.AreEqual("foo", result);

            appSettings.VerifyAll();
        }

        [TestMethod]
        public void GetEmailAddress_WhenOriginalStringIsMalformedEmail_ReturnsValueInLowerCase()
        {
            var appSettings = new Mock<IConfigurationSettings>(MockBehavior.Strict);
            appSettings.Setup(a => a.GetApplicationSetting(DomainAppSetting, null)).Returns("mycompany\\.com:othercompany.com");
            appSettings.Setup(a => a.GetApplicationSetting(UserRegexAppSetting, null)).Returns((string)null);
            appSettings.Setup(a => a.GetApplicationSetting(UserFixedAppSetting, null)).Returns((string)null);


            var logger = new Mock<ILogger>();

            var transformer = new UsernameTransformer(appSettings.Object, logger.Object);

            transformer.Initalize();

            var result = transformer.GetEmailAddress("FOO@");

            Assert.AreEqual("foo@", result);

            appSettings.VerifyAll();
        }

        [TestMethod]
        public void GetEmailAddress_WhenTransformExists_ReturnsTranslationInLowerCase()
        {
            var appSettings = new Mock<IConfigurationSettings>(MockBehavior.Strict);
            appSettings.Setup(a => a.GetApplicationSetting(DomainAppSetting, null)).Returns("mycompany\\.com:othercompany.com");
            appSettings.Setup(a => a.GetApplicationSetting(UserRegexAppSetting, null)).Returns((string)null);
            appSettings.Setup(a => a.GetApplicationSetting(UserFixedAppSetting, null)).Returns((string)null);


            var logger = new Mock<ILogger>();

            var transformer = new UsernameTransformer(appSettings.Object, logger.Object);

            transformer.Initalize();

            var result = transformer.GetEmailAddress("bob.vila@MYCOMPANY.com");

            Assert.AreEqual("bob.vila@othercompany.com", result);

            appSettings.VerifyAll();
        }

        [TestMethod]
        public void GetEmailAddress_WhenUserAndDomainTransformsExists_ReturnsTranslationInLowerCase()
        {
            var appSettings = new Mock<IConfigurationSettings>(MockBehavior.Strict);
            appSettings.Setup(a => a.GetApplicationSetting(DomainAppSetting, null)).Returns("mycompany\\.com:othercompany.com");
            appSettings.Setup(a => a.GetApplicationSetting(UserRegexAppSetting, null)).Returns(@"([A-Za-z]).+\.([A-Za-z]+):$1$2");
            appSettings.Setup(a => a.GetApplicationSetting(UserFixedAppSetting, null)).Returns((string)null);


            var logger = new Mock<ILogger>();

            var transformer = new UsernameTransformer(appSettings.Object, logger.Object);

            transformer.Initalize();

            var result = transformer.GetEmailAddress("bob.vila@MYCOMPANY.com");

            Assert.AreEqual("bvila@othercompany.com", result);

            appSettings.VerifyAll();
        }

        [TestMethod]
        public void GetEmailAddress_WhenUserDomainAndFixedTransformsExists_ReturnsTranslationInLowerCase()
        {
            var appSettings = new Mock<IConfigurationSettings>(MockBehavior.Strict);
            appSettings.Setup(a => a.GetApplicationSetting(DomainAppSetting, null)).Returns("mycompany\\.com:othercompany.com");
            appSettings.Setup(a => a.GetApplicationSetting(UserRegexAppSetting, null)).Returns(@"([A-Za-z]).+\.([A-Za-z]+):$1$2");
            appSettings.Setup(a => a.GetApplicationSetting(UserFixedAppSetting, null)).Returns("bvila:bavila");


            var logger = new Mock<ILogger>();

            var transformer = new UsernameTransformer(appSettings.Object, logger.Object);

            transformer.Initalize();

            var result = transformer.GetEmailAddress("bvila@MYCOMPANY.com");

            Assert.AreEqual("bavila@othercompany.com", result);

            appSettings.VerifyAll();
        }

        [TestMethod]
        public void GetEmailAddress_WhenDomainAndFixedTransformsExists_ReturnsTranslationInLowerCase()
        {
            var appSettings = new Mock<IConfigurationSettings>(MockBehavior.Strict);
            appSettings.Setup(a => a.GetApplicationSetting(DomainAppSetting, null)).Returns("mycompany\\.com:othercompany.com");
            appSettings.Setup(a => a.GetApplicationSetting(UserRegexAppSetting, null)).Returns((string)null);
            appSettings.Setup(a => a.GetApplicationSetting(UserFixedAppSetting, null)).Returns("bvila:bavila");

            var logger = new Mock<ILogger>();

            var transformer = new UsernameTransformer(appSettings.Object, logger.Object);

            transformer.Initalize();

            var result = transformer.GetEmailAddress("bvila@MYCOMPANY.com");

            Assert.AreEqual("bavila@othercompany.com", result);

            appSettings.VerifyAll();
        }

        [TestMethod]
        public void GetEmailAddress_WhenTransformDoesNotExist_ReturnsValueInLowerCase()
        {
            var appSettings = new Mock<IConfigurationSettings>(MockBehavior.Strict);
            appSettings.Setup(a => a.GetApplicationSetting(DomainAppSetting, null)).Returns("mycompany\\.com:othercompany.com");
            appSettings.Setup(a => a.GetApplicationSetting(UserRegexAppSetting, null)).Returns((string)null);
            appSettings.Setup(a => a.GetApplicationSetting(UserFixedAppSetting, null)).Returns((string)null);


            var logger = new Mock<ILogger>();

            var transformer = new UsernameTransformer(appSettings.Object, logger.Object);

            transformer.Initalize();

            var result = transformer.GetEmailAddress("bob.vila@othercompany.com");

            Assert.AreEqual("bob.vila@othercompany.com", result);

            appSettings.VerifyAll();
        }

        [TestMethod]
        public void GetEmailAddress_WhenMultipleTransformsExist_ReturnsTranslationInLowerCase()
        {
            var appSettings = new Mock<IConfigurationSettings>(MockBehavior.Strict);
            appSettings.Setup(a => a.GetApplicationSetting(DomainAppSetting, null)).Returns("mycompany\\.com:othercompany.com;cool\\.com:othercompany2.com");
            appSettings.Setup(a => a.GetApplicationSetting(UserRegexAppSetting, null)).Returns((string)null);
            appSettings.Setup(a => a.GetApplicationSetting(UserFixedAppSetting, null)).Returns((string)null);


            var logger = new Mock<ILogger>();

            var transformer = new UsernameTransformer(appSettings.Object, logger.Object);

            transformer.Initalize();

            var result = transformer.GetEmailAddress("bob.vila@cool.com");

            Assert.AreEqual("bob.vila@othercompany2.com", result);

            appSettings.VerifyAll();
        }
    }
}
