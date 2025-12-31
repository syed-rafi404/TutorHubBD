using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
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
        private Mock<UserManager<ApplicationUser>> GetMockUserManager()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            return new Mock<UserManager<ApplicationUser>>(store.Object, null, null, null, null, null, null, null, null);
        }

        [Fact]
        public async Task Index_ReturnsViewResult_WithListOfOffers()
        {
            var mockService = new Mock<ITuitionOfferService>();
            mockService.Setup(service => service.SearchOffersAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(new List<TuitionOffer>());
            var mockUserManager = GetMockUserManager();
            var mockNotificationService = new Mock<INotificationService>();
            var controller = new TuitionOfferController(mockService.Object, null, null, mockNotificationService.Object, mockUserManager.Object);

            var result = await controller.Index(null, null, null);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.IsAssignableFrom<List<TuitionOffer>>(viewResult.ViewData.Model);
        }

        [Fact]
        public async Task Create_Post_ReturnsRedirectToActionResult_WhenModelStateIsValid()
        {
            var mockService = new Mock<ITuitionOfferService>();
            var mockUserManager = GetMockUserManager();
            var mockNotificationService = new Mock<INotificationService>();
            mockUserManager.Setup(m => m.GetUserAsync(It.IsAny<System.Security.Claims.ClaimsPrincipal>()))
                .ReturnsAsync(new ApplicationUser { Id = "test-user-id" });
            var controller = new TuitionOfferController(mockService.Object, null, null, mockNotificationService.Object, mockUserManager.Object);
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

            var result = await controller.Create(viewModel);

            var redirectToActionResult = Assert.IsType<RedirectToActionResult>(result);
            Assert.Equal("Index", redirectToActionResult.ActionName);
            mockService.Verify(s => s.CreateOfferAsync(It.IsAny<TuitionOffer>()), Times.Once);
        }

        [Fact]
        public async Task Create_Post_ReturnsViewResult_WhenModelStateIsInvalid()
        {
            var mockService = new Mock<ITuitionOfferService>();
            var mockUserManager = GetMockUserManager();
            var mockNotificationService = new Mock<INotificationService>();
            var controller = new TuitionOfferController(mockService.Object, null, null, mockNotificationService.Object, mockUserManager.Object);
            controller.ModelState.AddModelError("Title", "Required");
            var viewModel = new TuitionOfferCreateViewModel();

            var result = await controller.Create(viewModel);

            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(viewModel, viewResult.Model);
            mockService.Verify(s => s.CreateOfferAsync(It.IsAny<TuitionOffer>()), Times.Never);
        }

        [Fact]
        public async Task ConfirmHiring_CreatesInvoice_WhenJobIsOpen()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: System.Guid.NewGuid().ToString())
                .Options;

            using (var context = new ApplicationDbContext(options))
            {
                context.Users.Add(new ApplicationUser 
                { 
                    Id = "user1", 
                    UserName = "tutor@test.com", 
                    Email = "tutor@test.com",
                    FullName = "Test Tutor",
                    Address = "Test Address",
                    Bio = "Test Bio"
                });
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
                var mockNotificationService = new Mock<INotificationService>();
                var mockUserManager = GetMockUserManager();
                
                var controller = new TuitionOfferController(mockService.Object, context, mockCommissionService.Object, mockNotificationService.Object, mockUserManager.Object);
                controller.TempData = new Mock<ITempDataDictionary>().Object;

                var result = await controller.ConfirmHiring(1, 10);

                mockCommissionService.Verify(s => s.CreateInvoiceAsync(1, 1000), Times.Once);
                
                var job = await context.TuitionOffers.FindAsync(1);
                Assert.Equal(JobStatus.Filled, job.Status);
                Assert.Equal(10, job.HiredTutorId);
            }
        }
    }
}
