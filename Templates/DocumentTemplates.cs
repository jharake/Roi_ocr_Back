using Roi_ocr.Models;
using System.Collections.Generic;

namespace Roi_ocr.Templates
{
    public class DocumentTemplates
    {
        public static List<DocumentTemplate> Templates => new List<DocumentTemplate>
    {
        new DocumentTemplate
        {
            Type = "ID",
            ReferenceImagePath = "./id/reference.png",
            Indices = new Dictionary<string, (double x1, double y1, double x2, double y2)>
            {
                { "الاسم", (0.7078, -0.0827, 0.8850, 0.0307) },
                { "الشهرة", (0.7177, 0.0257, 0.8514, 0.1402) },
                { "اسم الاب", (0.6642, 0.1422, 0.8293, 0.2308) },
                { "اسم الام وشهرتها", (0.4075, 0.2326, 0.7177, 0.3572) },
                { "محل الولادة", (0.4278, 0.4939, 0.7974, 0.6183) },
                { "تاريخ الولادة", (0.4514, 0.6402, 0.7626, 0.7328) }
            }
        },
        new DocumentTemplate
        {
            Type = "e5raj_aid",
            ReferenceImagePath = "./e5raj/reference.jpg",
            Indices = new Dictionary<string, (double x1, double y1, double x2, double y2)>
            {
                {"Rectangle 1", ( 0.8159,  0.2572, 0.9410, 0.2889) },
                {"Rectangle 2", ( 0.4519,  0.2554, 0.5780, 0.2924) },
                {"Rectangle 3", ( 0.8475,  0.1534, 0.9410, 0.1956) },
                {"Rectangle 4", ( 0.3858,  0.1463, 0.5790, 0.1991) },
                {"Rectangle 5", ( 0.8820,  0.4227, 0.9410, 0.4526) },
                {"Rectangle 6", ( 0.5088,  0.4174, 0.5780, 0.4508) },
                {"Rectangle 7", ( 0.8810,  0.5019, 0.9431, 0.5406) },
                {"Rectangle 8", ( 0.8658,  0.5828, 0.9431, 0.6180) },
                {"Rectangle 9", ( 0.5027,  0.5723, 0.5780, 0.6110) },
                { "Rectangle 10", ( 0.2393,  0.4860, 0.5769, 0.5318) },
            }
        }
    };
    }
}
