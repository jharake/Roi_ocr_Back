using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PaddleOCRSharp;
using System.Threading.Tasks;
using OpenCvSharp;
using Roi_ocr.Services;
using Roi_ocr.Helpers;
using Roi_ocr.Templates;

[Route("api/[controller]")]
[ApiController]
public class OcrController : ControllerBase
{
    private OCRResult ocrResult;
    private OCRModelConfig config;
    private OCRParameter ocrParameter;
    private PaddleOCREngine engine;
    private readonly ImageProcessingService _imageProcessingService;
    private readonly DatabaseHelper _databaseHelper;
    private readonly ImageProcessor _imageProcessor;


    public OcrController()
    {
        _databaseHelper = new DatabaseHelper();
        _imageProcessor = new ImageProcessor();
        _imageProcessingService = new ImageProcessingService();
        config = new OCRModelConfig();

        string rootpath = EngineBase.GetRootDirectory();

        string path = Path.Combine(rootpath, "inference");
        config.det_infer = Path.Combine(path, "ch_PP-OCRv4_det_infer");
        config.cls_infer = Path.Combine(path, "ch_ppocr_mobile_v2.0_cls_infer");
        config.rec_infer = Path.Combine(path, "arabic_PP-OCRv4_rec_infer");
        //config.rec_infer = Path.Combine(path, "ch_PP-OCRv4_rec_infer");
        config.keys = Path.Combine(path, "arabic_dict.txt");
        //config.keys = Path.Combine(path, "ppocr_keys.txt");


        ocrParameter = new OCRParameter();
        ocrResult = new OCRResult();
        engine = new PaddleOCREngine(config, ocrParameter);
    }

    [HttpPost("ocr-image")]
    public async Task<IActionResult> OcrImage([FromForm] IFormFile imageFile)
    {
        Console.WriteLine("OCR image request received.");
        if (imageFile == null || imageFile.Length == 0)
        {
            return BadRequest("Please upload a valid image file.");
        }

        var result = await PerformOcr(imageFile);
        return Ok(new { text = result });

    }
    [HttpPost("ocr-image-batch")]
    public async Task<IActionResult> OcrImageBatch([FromForm] IFormCollection formData)
    {
        var ocrResults = new List<string>();
        foreach (var file in formData.Files)
        {
            // Process each file (image) in the batch
            var result = await PerformOcr(file); 
            ocrResults.Add(result);
        }
        return Ok(new { text = ocrResults });
    }
    [HttpPost("extract-rois")]
    public IActionResult ExtractROIs([FromForm] IFormFile imageFile, [FromQuery] string documentType)
    {
        // Load the image from the uploaded file
        using var ms = new MemoryStream();
        imageFile.CopyTo(ms);
        byte[] imageData = ms.ToArray();
        Mat documentImage = Cv2.ImDecode(imageData, ImreadModes.Color);

        // Get the template
        var template = DocumentTemplates.Templates.Find(t => t.Type == documentType);
        if (template == null) return BadRequest("Invalid document type");

        // Get the reference transformation
        var bottomRight = _imageProcessingService.GetReferenceImageTransformation(documentImage, template.ReferenceImagePath);

        // Extract ROIs
        var rois = _imageProcessingService.ExtractROIs(documentImage, template,bottomRight);

        // Return the ROIs (base64 or file paths for simplicity)
        var roiResults = new Dictionary<string, string>();
        foreach (var roi in rois)
        {
            string base64 = Convert.ToBase64String(roi.Value.ToBytes(".png"));
            roiResults[roi.Key] = base64;
        }

        return Ok(roiResults);
    }

    [HttpPost("classify-and-extract-rois")]
    public async Task<IActionResult> ClassifyAndExtractROIs([FromForm] IFormFile imageFile)
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

        // Get the stored images and their classes
        var storedImages = _databaseHelper.GetImagePathsAndClasses();

        // classification
        var classificationResult = _imageProcessor.ClassifyImage(tempFilePath, storedImages);

        // Clean
        if (System.IO.File.Exists(tempFilePath))
        {
            System.IO.File.Delete(tempFilePath);
        }

        var documentType = classificationResult;

        using var ms = new MemoryStream();
        imageFile.CopyTo(ms);
        byte[] imageData = ms.ToArray();
        Mat documentImage = Cv2.ImDecode(imageData, ImreadModes.Color);

        var template = DocumentTemplates.Templates.Find(t => t.Type == documentType);
        if (template == null) return BadRequest("Invalid document type");

        var bottomRight = _imageProcessingService.GetReferenceImageTransformation(documentImage, template.ReferenceImagePath);
        var rois = _imageProcessingService.ExtractROIs(documentImage, template, bottomRight);

        var roiResults = new Dictionary<string, object>();
        foreach (var roi in rois)
        {
            // Perform OCR on each ROI
            var roiImage = roi.Value;
            var roiImageBytes = roiImage.ToBytes(".png");

            // Convert ROI to IFormFile
            var roiFormFile = new FormFile(new MemoryStream(roiImageBytes), 0, roiImageBytes.Length, roi.Key, $"{roi.Key}.png");

            var roiOcrResult = await PerformOcr(roiFormFile);

            // Convert ROI image to base64
            var roiBase64 = Convert.ToBase64String(roiImageBytes);

            roiResults[roi.Key] = new { Text = roiOcrResult, ImageBase = roiBase64 };
        }

        return Ok(new { ROIs = roiResults });
    }

    private async Task<string> PerformOcr(IFormFile file)
    {
        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        var imageBytes = memoryStream.ToArray();

        Mat image = Cv2.ImDecode(imageBytes, ImreadModes.Color);
        if (image.Empty())
        {
            return $"Failed to decode image from {file.FileName}.";
        }

        // image processing 
        Mat processedImage = image.Clone();
        Cv2.FastNlMeansDenoisingColored(processedImage, processedImage, 10, 10, 7, 21);
        Cv2.GaussianBlur(processedImage, processedImage, new Size(5, 5), 0);
        Cv2.CvtColor(processedImage, processedImage, ColorConversionCodes.BGR2GRAY);
        Cv2.MorphologyEx(processedImage, processedImage, MorphTypes.Close, Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3)));

        // Convert to byte array
        byte[] processedImageBytes;
        using (var processedMemoryStream = new MemoryStream())
        {
            Cv2.ImEncode(".png", processedImage, out processedImageBytes);
            processedMemoryStream.Write(processedImageBytes, 0, processedImageBytes.Length);
            processedImageBytes = processedMemoryStream.ToArray();
        }

        // OCR
        ocrResult = engine.DetectText(processedImageBytes);

        if (ocrResult != null && !string.IsNullOrEmpty(ocrResult.Text))
        {
            string recognizedText = ocrResult.Text;

            // Reverse if Arabic
            // if (recognizedText.Any(c => c >= '\u0600' && c <= '\u06FF'))
            // {
            //     recognizedText = new string(recognizedText.Reverse().ToArray());
            // }

            return recognizedText;
        }

        return $"OCR failed for {file.FileName}.";
    }
    
}
