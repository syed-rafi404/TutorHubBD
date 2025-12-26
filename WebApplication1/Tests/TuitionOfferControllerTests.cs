using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Moq;
using TutorHubBD.Web.Controllers;
using TutorHubBD.Web.Data;
using TutorHubBD.Web.Models;
using TutorHubBD.Web.Models.ViewModels;
using TutorHubBD.Web.Services;
using Xunit;

namespace TutorHubBD.Web.Tests
{
    public class TuitionOfferControllerTests
    {
        [Fact]
        public async Task Index_ReturnsViewResult_WithListOfOffers()
        {
            // Arrange
            var mockService = new Mock<ITuitionOfferService>();
            mockService.Setup(service => service.SearchOffersAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<TuitionOffer>());
            var controller = new TuitionOfferController(mockService.Object, null, null);

            // Act
            var result = await controller.Index(null, null, null);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            var model = Assert.IsAssignableFrom<List<TuitionOffer>>(viewResult.ViewData.Model);
        }

        [Fact]
        public async Task Create_Post_ReturnsRedirectToActionResult_WhenModelStateIsValid()
        {
            // Arrange
            var mockService = new Mock<ITuitionOfferService>();
            var controller = new TuitionOfferController(mockService.Object, null, null);
            var viewModel = new TuitionOfferCreateViewModel
            {
                Title = "Test Title",
                Description = "Test Description",
                Salary = 5000,
                City = "Dhaka",
                Location = "Mirpur",
                Medium = "English",
                StudentClass = "Class 10"
            };

            // Act
            var result = await controller.Create(viewModel);

            // Assert
            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            mockService.Verify(s => s.CreateOfferAsync(It.IsAny<TuitionOffer>()), Times.Once);
        }

        [Fact]
        public async Task Create_Post_ReturnsViewResult_WhenModelStateIsInvalid()
        {
            // Arrange
            var mockService = new Mock<ITuitionOfferService>();
            var controller = new TuitionOfferController(mockService.Object, null, null);
            controller.ModelState.AddModelError("Title", "Required");
            var viewModel = new TuitionOfferCreateViewModel();

            // Act
            var result = await controller.Create(viewModel);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(viewModel, viewResult.Model);
            mockService.Verify(s => s.CreateOfferAsync(It.IsAny<TuitionOffer>()), Times.Never);
        }

        [Fact]
        public async Task ConfirmHiring_CreatesInvoice_WhenJobIsOpen()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: "ConfirmHiring_CreatesInvoice")
                .Options;

            // Seed data
            using (var context = new ApplicationDbContext(options))
            {
                context.TuitionOffers.Add(new TuitionOffer
                {
                    Id = 1,
                    Title = "Math Tutor",
                    Salary = 1000,
                    Status = JobStatus.Open,
                    Description = "Need a math tutor",
                    City = "Dhaka",
                    Location = "Mirpur",
                    Medium = "English",
                    StudentClass = "Class 10",
                    Subject = "Math",
                    DaysPerWeek = "3 days",
                    GenderPreference = "Any"
                });
                context.Tutors.Add(new Tutor { TutorID = 10, Education = "BSc", Subjects = "Math", Rating = 5, IsVerified = true, UserId = "user1" });
                await context.SaveChangesAsync();
            }

            using (var context = new ApplicationDbContext(options))
            {
                var mockService = new Mock<ITuitionOfferService>();
                var mockCommissionService = new Mock<ICommissionService>();
                
                var controller = new TuitionOfferController(mockService.Object, context, mockCommissionService.Object);
                // Mock TempData
                controller.TempData = new Mock<ITempDataDictionary>().Object;

                // Act
                var result = await controller.ConfirmHiring(1, 10);

                // Assert
                // Verify invoice creation was called with correct parameters
                mockCommissionService.Verify(s => s.CreateInvoiceAsync(1, 1000), Times.Once);
                
                // Verify job status updated
                var job = await context.TuitionOffers.FindAsync(1);
                Assert.Equal(JobStatus.Filled, job.Status);
                Assert.Equal(10, job.HiredTutorId);
            }
        }
    }
}
