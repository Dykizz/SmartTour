/**
 * audioHelper.js
 * JS Interop helper để điều khiển audio từ Blazor/C#.
 * Singleton <audio> element — tránh phát nhiều file cùng lúc.
 *
 * FIX AUTOPLAY: WebView Android chặn audio nếu không có user gesture.
 * Giải pháp: phát muted trước, unmute ngay khi được phép.
 */
window.audioHelper = (() => {
    let _audio = null;
    let _dotNetRef = null;

    function _getOrCreate() {
        if (!_audio) {
            _audio = document.createElement('audio');
            _audio.id = '__geofence-audio__';
            _audio.preload = 'auto';
            
            // Thông báo cho Blazor khi kết thúc audio
            _audio.addEventListener('ended', () => {
                if (_dotNetRef) {
                    _dotNetRef.invokeMethodAsync('OnAudioEnded');
                }
            });

            document.body.appendChild(_audio);
        }
        return _audio;
    }

    // Đăng ký nhận DotNetReference từ Blazor
    function setDotNetReference(dotNetRef) {
        _dotNetRef = dotNetRef;
        console.log('[audioHelper] DotNetReference set');
    }

    // Được gọi 1 lần sau lần đầu user chạm màn hình — mở khoá autoplay
    function _unlockAudio() {
        const audio = _getOrCreate();
        audio.muted = true;
        audio.play().then(() => {
            audio.pause();
            audio.muted = false;
            audio.currentTime = 0;
        }).catch(() => { });
        document.removeEventListener('touchstart', _unlockAudio);
        document.removeEventListener('click', _unlockAudio);
    }

    // Đăng ký unlock ngay khi script load
    document.addEventListener('touchstart', _unlockAudio, { once: true });
    document.addEventListener('click', _unlockAudio, { once: true });

    return {
        /**
         * Phát một URL audio. Tự động xử lý lỗi autoplay policy.
         * @param {string} url
         */
        play: async function (url) {
            const audio = _getOrCreate();
            if (!audio.paused) {
                audio.pause();
            }
            audio.src = url;
            audio.muted = false;
            audio.currentTime = 0;
            try {
                await audio.play();
                console.log('[audioHelper] Playing:', url);
            } catch (err) {
                // NotAllowedError: thử phát muted rồi unmute
                if (err.name === 'NotAllowedError') {
                    console.warn('[audioHelper] Autoplay blocked, trying muted...');
                    audio.muted = true;
                    try {
                        await audio.play();
                        // Unmute sau 300ms (đủ để WebView cho phép)
                        setTimeout(() => { audio.muted = false; }, 300);
                    } catch (e2) {
                        console.error('[audioHelper] Muted play also failed:', e2);
                    }
                } else {
                    console.error('[audioHelper] Error:', err);
                }
            }
        },

        /**
         * Dừng audio đang phát (reset về đầu).
         */
        stop: function () {
            if (_audio && !_audio.paused) {
                _audio.pause();
                _audio.currentTime = 0;
            }
        },

        /**
         * Phát file audio mới tại một mốc thời gian cụ thể (dùng khi đổi ngôn ngữ).
         * @param {string} url - URL file audio mới
         * @param {number} startTime - Giây bắt đầu (0 = từ đầu)
         */
        playAtTime: async function (url, startTime) {
            const audio = _getOrCreate();
            audio.src = url;
            audio.currentTime = startTime;
            audio.muted = false;
            try {
                await audio.play();
                console.log('[audioHelper] playAtTime:', url, 'at', startTime, 's');
            } catch (err) {
                console.error('[audioHelper] playAtTime error:', err);
            }
        },

        /**
         * Tạm dừng audio hiện tại mà không reset thời gian.
         */
        pause: function () {
            if (_audio) {
                _audio.pause();
            }
        },

        /**
         * Tiếp tục phát từ vị trí đang tạm dừng.
         */
        resume: function () {
            if (_audio && _audio.src) {
                _audio.play().catch(e => console.error('[audioHelper] Resume error:', e));
            }
        },

        /**
         * Kiểm tra xem audio đang phát không.
         * @returns {boolean}
         */
        isPlaying: function () {
            return _audio != null && !_audio.paused;
        },

        /**
         * Phát audio dùng thẻ <audio> có sẵn theo ID (cho PoiDetail player).
         * @param {string} elementId
         */
        playById: function (elementId) {
            const el = document.getElementById(elementId);
            if (el) el.play().catch(e => console.error('[audioHelper] playById error:', e));
        },

        /**
         * Lấy vị trí thời gian hiện tại của audio (giây).
         * @returns {number}
         */
        getCurrentTime: function () {
            return _audio ? _audio.currentTime : 0;
        },

        /**
         * Pause audio theo ID.
         * @param {string} elementId
         */
        pauseById: function (elementId) {
            const el = document.getElementById(elementId);
            if (el) el.pause();
        },

        setDotNetReference: setDotNetReference
    };
})();
