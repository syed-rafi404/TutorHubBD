using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Moq;
using TutorHubBD.Web.Controllers;
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
            var controller = new TuitionOfferController(mockService.Object);

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
            var controller = new TuitionOfferController(mockService.Object);
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
            var controller = new TuitionOfferController(mockService.Object);
            controller.ModelState.AddModelError("Title", "Required");
            var viewModel = new TuitionOfferCreateViewModel();

            // Act
            var result = await controller.Create(viewModel);

            // Assert
            var viewResult = Assert.IsType<ViewResult>(result);
            Assert.Equal(viewModel, viewResult.Model);
            mockService.Verify(s => s.CreateOfferAsync(It.IsAny<TuitionOffer>()), Times.Never);
        }
    }
}
