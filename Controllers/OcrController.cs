using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PaddleOCRSharp;
using System.Threading.Tasks;
using OpenCvSharp;

[Route("api/[controller]")]
[ApiController]
public class OcrController : ControllerBase
{
    private OCRResult ocrResult;
    private OCRModelConfig config;
    private OCRParameter ocrParameter;
    private PaddleOCREngine engine;

    public OcrController()
    {
        config = new OCRModelConfig();

        string rootpath = EngineBase.GetRootDirectory();
        Console.WriteLine($"Root path: {rootpath}");

        string path = Path.Combine(rootpath, "inference");
        config.det_infer = Path.Combine(path, "ch_PP-OCRv4_det_infer");
        config.cls_infer = Path.Combine(path, "ch_ppocr_mobile_v2.0_cls_infer");
        config.rec_infer = Path.Combine(path, "arabic_PP-OCRv4_rec_infer");
        config.keys = Path.Combine(path, "arabic_dict.txt");


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
    private async Task<string> PerformOcr(IFormFile file)
    {
        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        var imageBytes = memoryStream.ToArray();

        Mat image = Cv2.ImDecode(imageBytes, ImreadModes.Color);
        // Apply the image processing steps
        Mat roi = image.Clone();
        Cv2.FastNlMeansDenoisingColored(roi, roi, 10, 10, 7, 21);
        Cv2.GaussianBlur(roi, roi, new Size(5, 5), 0);
        Cv2.CvtColor(roi, roi, ColorConversionCodes.BGR2GRAY);
        Cv2.MorphologyEx(roi, roi, MorphTypes.Close, Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3)));
        Cv2.GaussianBlur(roi, roi, new Size(5, 5), 0);

        // Convert to byte array
        byte[] processedImageBytes;
        using (var processedMemoryStream = new MemoryStream())
        {
            Cv2.ImEncode(".jpg", roi, out processedImageBytes);
        }
        // Detect text 
        ocrResult = engine.DetectText(imageBytes);

        if (ocrResult != null && !string.IsNullOrEmpty(ocrResult.Text))
        {
            string recognizedText = ocrResult.Text;

            // reverse if arabic
            if (recognizedText.Any(c => c >= '\u0600' && c <= '\u06FF'))
            {
                recognizedText = new string(recognizedText.Reverse().ToArray());
            }

            return recognizedText;
        }

        return $"OCR failed for {file.FileName}.";
    }
}
