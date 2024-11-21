using System.Collections.Generic;

namespace Roi_ocr.Models
{
    public class DocumentTemplate
    {
        public string Type { get; set; } = string.Empty;
        public Dictionary<string, (double X1, double Y1, double X2, double Y2)> Indices { get; set; } = new();
        public string ReferenceImagePath { get; set; } = string.Empty; 
    }
}
