using System;

namespace SmartTour.Mobile.Services;

public class LanguageService
{
    public string SelectedLanguage { get; private set; } = "vi";
    public event Action? OnLanguageChanged;

    // Từ điển chứa tất cả bản dịch UI
    private readonly Dictionary<string, Dictionary<string, string>> _translations = new()
    {
        ["vi"] = new() {
            ["Home"] = "Trang Chủ",
            ["Hello"] = "Xin chào",
            ["SearchHint"] = "Hôm nay bạn muốn đi đâu?",
            ["Categories"] = "Danh mục phổ biến",
            ["All"] = "Tất cả",
            ["Featured"] = "Địa điểm nổi bật",
            ["SeeAll"] = "Xem tất cả",
            ["FindAround"] = "Tìm quanh đây:",
            ["Details"] = "Chi tiết",
            ["NoResults"] = "Chưa tìm thấy địa điểm",
            ["EndList"] = "Hết danh sách",
            ["Map"] = "Bản đồ",
            ["Profile"] = "Cá nhân",
            ["Logout"] = "Thoát",
            ["Login"] = "Đăng nhập",
            ["Introduction"] = "Giới thiệu",
            ["OpeningHours"] = "Giờ mở cửa",
            ["Location"] = "Vị trí",
            ["Direction"] = "Dẫn đường",
            ["Audio"] = "Thuyết minh",
            ["Share"] = "Chia sẻ",
            ["Favorite"] = "Yêu thích",
            ["StartJourney"] = "BẮT ĐẦU HÀNH TRÌNH",
            ["Updating"] = "Đang cập nhật nội dung...",
            ["ViewOnMap"] = "Xem trên bản đồ",
            ["Coordinates"] = "Tọa độ",
            ["Place"] = "Địa điểm",
            // Danh mục từ API
            ["Khác"] = "Khác",
            ["Quán Bar/Pub"] = "Quán Bar/Pub",
            ["Quán ăn nhanh"] = "Quán ăn nhanh",
            ["Nhà hàng"] = "Nhà hàng",
            ["Cà phê"] = "Cà phê"
        },
        ["en"] = new() {
            ["Home"] = "Home",
            ["Hello"] = "Hello",
            ["SearchHint"] = "Where do you want to go today?",
            ["Categories"] = "Popular Categories",
            ["All"] = "All",
            ["Featured"] = "Featured Places",
            ["SeeAll"] = "See all",
            ["FindAround"] = "Find around here:",
            ["Details"] = "Details",
            ["NoResults"] = "No places found",
            ["EndList"] = "End of list",
            ["Map"] = "Map",
            ["Profile"] = "Profile",
            ["Logout"] = "Logout",
            ["Login"] = "Login",
            ["Introduction"] = "Introduction",
            ["OpeningHours"] = "Opening Hours",
            ["Location"] = "Location",
            ["Direction"] = "Direction",
            ["Audio"] = "Audio",
            ["Share"] = "Share",
            ["Favorite"] = "Favorite",
            ["StartJourney"] = "START JOURNEY",
            ["Updating"] = "Updating content...",
            ["ViewOnMap"] = "View on map",
            ["Coordinates"] = "Coordinates",
            ["Place"] = "Place",
            // Danh mục từ API
            ["Khác"] = "Other",
            ["Quán Bar/Pub"] = "Bar/Pub",
            ["Quán ăn nhanh"] = "Fast Food",
            ["Nhà hàng"] = "Restaurant",
            ["Cà phê"] = "Cafe"
        }
        // Bạn có thể thêm ["fr"], ["jp"] vào đây cực kỳ dễ dàng
    };

    public void SetLanguage(string languageCode)
    {
        if (SelectedLanguage != languageCode)
        {
            SelectedLanguage = languageCode;
            OnLanguageChanged?.Invoke();
        }
    }

    // Hàm lấy bản dịch tự động
    public string T(string key)
    {
        if (_translations.ContainsKey(SelectedLanguage) && _translations[SelectedLanguage].ContainsKey(key))
        {
            return _translations[SelectedLanguage][key];
        }
        
        // Nếu không tìm thấy ngôn ngữ đang chọn, thử lấy tiếng Việt
        if (_translations["vi"].ContainsKey(key)) return _translations["vi"][key];
        
        return key; // Cùng lắm trả về chính cái key đó
    }
}
