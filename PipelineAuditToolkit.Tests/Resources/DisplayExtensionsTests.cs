using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PipelineAuditToolkit.Resources;

namespace PipelineAuditToolkit.Tests
{
    [TestClass]
    public class DisplayExtensionsTests
    {
        [TestMethod]
        public void ToReportDateTime_ReturnsFormattedString()
        {
            var date = new DateTime(2016, 8, 12, 12, 22, 22);
            var result = date.ToReportDateTime();
            
            Assert.AreEqual("08/12/2016 12:22 PM", result);
        }

        [TestMethod]
        public void ToEndOfDay_WhenTimeDoesNotExist_ReturnsEndOfDay()
        {
            var date = new DateTime(2016, 8, 12);
            var result = date.ToEndOfDay();

            Assert.AreEqual(11, result.Hour);
            Assert.AreEqual(59, result.Minute);
            Assert.AreEqual(59, result.Second);
            Assert.AreEqual(0, result.Millisecond);
        }

        [TestMethod]
        public void ToEndOfDay_WhenTimeDoesExist_ReturnsEndOfDay()
        {
            var date = new DateTime(2016, 8, 12, 12, 22, 22);
            var result = date.ToEndOfDay();

            Assert.AreEqual(11, result.Hour);
            Assert.AreEqual(59, result.Minute);
            Assert.AreEqual(59, result.Second);
            Assert.AreEqual(0, result.Millisecond);
        }
    }
}
