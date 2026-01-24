using Microsoft.AspNetCore.Mvc;
using SmartTour.Shared;

namespace SmartTour.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ToursController : ControllerBase
{
    private static readonly List<Tour> Tours = new()
    {
        new Tour 
        { 
            Id = 1, 
            Title = "Khám phá Phố Cổ Hội An", 
            Description = "Tour đi bộ xuyên qua các con phố cổ kính của Hội An.",
            PointsOfInterest = new List<Poi>
            {
                new Poi { Id = 101, Name = "Chùa Cầu", Description = "Biểu tượng mang tính lịch sử của Hội An.", Latitude = 15.8771, Longitude = 108.3259 },
                new Poi { Id = 102, Name = "Nhà cổ Tấn Ký", Description = "Nhà cổ hơn 200 năm tuổi.", Latitude = 15.8762, Longitude = 108.3265 }
            }
        },
        new Tour 
        { 
            Id = 2, 
            Title = "Ẩm thực đường phố Sài Gòn", 
            Description = "Trải nghiệm các món ăn đặc trưng tại Quận 1.",
            PointsOfInterest = new List<Poi>
            {
                new Poi { Id = 201, Name = "Chợ Bến Thành", Description = "Trung tâm giao thương sầm uất.", Latitude = 10.7719, Longitude = 106.6983 }
            }
        }
    };

    [HttpGet]
    public ActionResult<IEnumerable<Tour>> GetTours()
    {
        return Ok(Tours);
    }

    [HttpGet("{id}")]
    public ActionResult<Tour> GetTour(int id)
    {
        var tour = Tours.FirstOrDefault(t => t.Id == id);
        if (tour == null) return NotFound();
        return Ok(tour);
    }
}
