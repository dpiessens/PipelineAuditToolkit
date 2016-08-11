using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using PipelineAuditToolkit.Providers;
using PipelineAuditToolkit.Utility;

namespace PipelineAuditToolkit.Tests
{
    [TestClass]
    public class UsernameTransformerTests
    {
        private const string AppSetting = "UserNameTransformer.Translations";

        [TestMethod]
        public void Initialize_WhenNoDataExists_CompletesWithoutError()
        {
            var appSettings = new Mock<IConfigurationSettings>(MockBehavior.Strict);
            appSettings.Setup(a => a.GetApplicationSetting(AppSetting, null)).Returns((string)null);

            var logger = new Mock<ILogger>();

            var transformer = new UsernameTransformer(appSettings.Object, logger.Object);

            transformer.Initalize();

            Assert.AreEqual(0, transformer.RuleCount);
            
            appSettings.VerifyAll();
        }

        [TestMethod]
        public void Initialize_WhenInvalidDataExists_CompletesWithoutErrorButIgnoresRule()
        {
            var appSettings = new Mock<IConfigurationSettings>(MockBehavior.Strict);
            appSettings.Setup(a => a.GetApplicationSetting(AppSetting, null)).Returns("mycompany\\.com");

            var logger = new Mock<ILogger>();

            var transformer = new UsernameTransformer(appSettings.Object, logger.Object);

            transformer.Initalize();

            Assert.AreEqual(0, transformer.RuleCount);

            appSettings.VerifyAll();
        }

        [TestMethod]
        public void Initialize_WhenValidDataExists_CompletesWithoutError()
        {
            var appSettings = new Mock<IConfigurationSettings>(MockBehavior.Strict);
            appSettings.Setup(a => a.GetApplicationSetting(AppSetting, null)).Returns("mycompany\\.com:othercompany.com");

            var logger = new Mock<ILogger>();

            var transformer = new UsernameTransformer(appSettings.Object, logger.Object);

            transformer.Initalize();

            Assert.AreEqual(1, transformer.RuleCount);

            appSettings.VerifyAll();
        }

        [TestMethod]
        public void Initialize_WhenRegexIsMalformed_CompletesWithoutErrorButIgnoresRule()
        {
            var appSettings = new Mock<IConfigurationSettings>(MockBehavior.Strict);
            appSettings.Setup(a => a.GetApplicationSetting(AppSetting, null)).Returns("mycompany[\\.com:othercompany.com");

            var logger = new Mock<ILogger>();

            var transformer = new UsernameTransformer(appSettings.Object, logger.Object);

            transformer.Initalize();

            Assert.AreEqual(0, transformer.RuleCount);

            appSettings.VerifyAll();
        }

        [TestMethod]
        public void GetEmailAddress_WhenOriginalStringIsNotAnEmail_ReturnsValueInLowerCase()
        {
            var appSettings = new Mock<IConfigurationSettings>(MockBehavior.Strict);
            appSettings.Setup(a => a.GetApplicationSetting(AppSetting, null)).Returns("mycompany\\.com:othercompany.com");

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
            appSettings.Setup(a => a.GetApplicationSetting(AppSetting, null)).Returns("mycompany\\.com:othercompany.com");

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
            appSettings.Setup(a => a.GetApplicationSetting(AppSetting, null)).Returns("mycompany\\.com:othercompany.com");

            var logger = new Mock<ILogger>();

            var transformer = new UsernameTransformer(appSettings.Object, logger.Object);

            transformer.Initalize();

            var result = transformer.GetEmailAddress("bob.vila@MYCOMPANY.com");

            Assert.AreEqual("bob.vila@othercompany.com", result);

            appSettings.VerifyAll();
        }

        [TestMethod]
        public void GetEmailAddress_WhenTransformDoesNotExist_ReturnsValueInLowerCase()
        {
            var appSettings = new Mock<IConfigurationSettings>(MockBehavior.Strict);
            appSettings.Setup(a => a.GetApplicationSetting(AppSetting, null)).Returns("mycompany\\.com:othercompany.com");

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
            appSettings.Setup(a => a.GetApplicationSetting(AppSetting, null)).Returns("mycompany\\.com:othercompany.com;cool\\.com:othercompany2.com");

            var logger = new Mock<ILogger>();

            var transformer = new UsernameTransformer(appSettings.Object, logger.Object);

            transformer.Initalize();

            var result = transformer.GetEmailAddress("bob.vila@cool.com");

            Assert.AreEqual("bob.vila@othercompany2.com", result);

            appSettings.VerifyAll();
        }
    }
}
