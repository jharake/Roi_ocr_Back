using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Roi_ocr.Helpers;
using Roi_ocr.Services;
using System.IO;

namespace Roi_ocr.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImageClassificationController : ControllerBase
    {
        private readonly DatabaseHelper _databaseHelper;
        private readonly ImageProcessor _imageProcessor;

        public ImageClassificationController(DatabaseHelper databaseHelper, ImageProcessor imageProcessor)
        {
            _databaseHelper = databaseHelper;
            _imageProcessor = imageProcessor;
        }

        [HttpPost("classify")]
        public IActionResult ClassifyImage([FromForm] IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                return BadRequest("No image file uploaded.");
            }

            // temp
            var tempFilePath = Path.GetTempFileName();
            using (var stream = new FileStream(tempFilePath, FileMode.Create))
            {
                imageFile.CopyTo(stream);
            }

            var storedImages = _databaseHelper.GetImagePathsAndClasses();

            // classification
            var classificationResult = _imageProcessor.ClassifyImage(tempFilePath, storedImages);

            // Clean
            if (System.IO.File.Exists(tempFilePath))
            {
                System.IO.File.Delete(tempFilePath);
            }

            return Ok(new { Message = classificationResult });
        }
    }
}
