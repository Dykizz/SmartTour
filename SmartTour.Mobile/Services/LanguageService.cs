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
            ["Scan"] = "Quét QR",
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
            ["Cà phê"] = "Cà phê",
            // Auto-Play
            ["AutoPlayTitle"] = "Tự động thuyết minh",
            ["AutoPlayDesc"] = "Tự động phát khi đến gần địa điểm",
            // Days
            ["Monday"] = "Thứ 2",
            ["Tuesday"] = "Thứ 3",
            ["Wednesday"] = "Thứ 4",
            ["Thursday"] = "Thứ 5",
            ["Friday"] = "Thứ 6",
            ["Saturday"] = "Thứ 7",
            ["Sunday"] = "Chủ Nhật",
            ["Narrating"] = "Đang thuyết minh...",
            ["NearbyArea"] = "Bạn đang ở gần",
            ["Replay"] = "Nghe lại"
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
            ["Scan"] = "Scan QR",
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
            ["Cà phê"] = "Cafe",
            // Tự động phát
            ["AutoPlayTitle"] = "Auto-Play Commentary",
            ["AutoPlayDesc"] = "Play audio when near POIs",
            // Thứ trong tuần
            ["Monday"] = "Monday",
            ["Tuesday"] = "Tuesday",
            ["Wednesday"] = "Wednesday",
            ["Thursday"] = "Thursday",
            ["Friday"] = "Friday",
            ["Saturday"] = "Saturday",
            ["Sunday"] = "Sunday",
            ["Narrating"] = "Narrating...",
            ["NearbyArea"] = "You are nearby",
            ["Replay"] = "Replay"
        },
        ["ko"] = new() {
            ["Home"] = "홈",
            ["Hello"] = "안녕하세요",
            ["SearchHint"] = "오늘은 어디로 가고 싶으신가요?",
            ["Categories"] = "인기 카테고리",
            ["All"] = "전체",
            ["Featured"] = "추천 장소",
            ["SeeAll"] = "전체보기",
            ["FindAround"] = "주변 찾기:",
            ["Details"] = "상세보기",
            ["NoResults"] = "결과를 찾을 수 없습니다",
            ["EndList"] = "리스트의 끝",
            ["Map"] = "지도",
            ["Profile"] = "프로필",
            ["Scan"] = "QR 스캔",
            ["Logout"] = "로그아웃",
            ["Login"] = "로그인",
            ["Introduction"] = "소개",
            ["OpeningHours"] = "영업 시간",
            ["Location"] = "위치",
            ["Direction"] = "길 찾기",
            ["Audio"] = "오디오 가이드",
            ["Share"] = "공유하기",
            ["Favorite"] = "찜하기",
            ["StartJourney"] = "여행 시작하기",
            ["Updating"] = "콘텐츠 업데이트 중...",
            ["ViewOnMap"] = "지도로 보기",
            ["Coordinates"] = "좌표",
            ["Place"] = "장소",
            // Categories
            ["Khác"] = "기타",
            ["Quán Bar/Pub"] = "바/펍",
            ["Quán ăn nhanh"] = "패스트푸드",
            ["Nhà hàng"] = "레스토랑",
            ["Cà phê"] = "카페",
            // Auto-Play
            ["AutoPlayTitle"] = "자동 음성 안내",
            ["AutoPlayDesc"] = "장소 근처에서 오디오 자동 재생",
            // Days
            ["Monday"] = "월요일",
            ["Tuesday"] = "화요일",
            ["Wednesday"] = "수요일",
            ["Thursday"] = "목요일",
            ["Friday"] = "금요일",
            ["Saturday"] = "토요일",
            ["Sunday"] = "일요일",
            ["Narrating"] = "음성 안내 중...",
            ["NearbyArea"] = "주변에 있습니다",
            ["Replay"] = "다시 듣기"
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
