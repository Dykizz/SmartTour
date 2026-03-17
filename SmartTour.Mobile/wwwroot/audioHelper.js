/**
 * audioHelper.js
 * JS Interop helper để điều khiển audio từ Blazor/C#.
 * Hỗ trợ hàng đợi (queue) — phát lần lượt, không đè lên nhau.
 */
window.audioHelper = (() => {
    let _audio = null;
    let _dotNetRef = null;
    let _queue = [];     // Hàng đợi URL audio
    let _isPlaying = false;  // Trạng thái đang phát

    // ── Khởi tạo element audio ────────────────────────────────────────────
    function _getOrCreate() {
        if (!_audio) {
            _audio = document.createElement('audio');
            _audio.id = '__geofence-audio__';
            _audio.preload = 'auto';

            _audio.addEventListener('ended', () => {
                console.log('[audioHelper] ⏹️ Bài kết thúc, queue còn:', _queue.length);
                _isPlaying = false;

                // Báo Blazor
                if (_dotNetRef) {
                    _dotNetRef.invokeMethodAsync('OnAudioEnded');
                }

                // Tự phát bài tiếp theo
                _playNext();
            });

            document.body.appendChild(_audio);
        }
        return _audio;
    }

    // ── Unlock autoplay (Android WebView) ────────────────────────────────
    function _unlockAudio() {
        const audio = _getOrCreate();
        audio.muted = true;
        audio.play().then(() => {
            audio.pause();
            audio.muted = false;
            audio.currentTime = 0;
        }).catch(() => { });
    }
    document.addEventListener('touchstart', _unlockAudio, { once: true });
    document.addEventListener('click', _unlockAudio, { once: true });

    // ── Phát bài đầu tiên trong queue ────────────────────────────────────
    async function _playNext() {
        if (_isPlaying || _queue.length === 0) return;

        const url = _queue.shift();
        _isPlaying = true;

        const audio = _getOrCreate();
        audio.src = url;
        audio.muted = false;
        audio.currentTime = 0;

        console.log(`[audioHelper] 🎵 Đang phát: ${url} | Queue còn: ${_queue.length}`);

        try {
            await audio.play();
        } catch (err) {
            if (err.name === 'NotAllowedError') {
                console.warn('[audioHelper] Autoplay bị chặn, thử muted...');
                audio.muted = true;
                try {
                    await audio.play();
                    setTimeout(() => { audio.muted = false; }, 300);
                } catch (e2) {
                    console.error('[audioHelper] Muted cũng thất bại:', e2);
                    _isPlaying = false;
                    _playNext(); // Bỏ qua → phát bài tiếp
                }
            } else {
                console.error('[audioHelper] Lỗi phát:', err);
                _isPlaying = false;
                _playNext();
            }
        }
    }

    // ── Đăng ký DotNetReference ──────────────────────────────────────────
    function setDotNetReference(dotNetRef) {
        _dotNetRef = dotNetRef;
        console.log('[audioHelper] DotNetReference đã set');
    }

    // ── Public API ───────────────────────────────────────────────────────
    return {

        /**
         * Thêm URL vào queue và bắt đầu phát nếu đang rảnh.
         * Nếu đang phát → đưa vào queue chờ.
         */
        play: async function (url) {
            // Tránh thêm trùng URL vào queue
            if (_queue.includes(url)) {
                console.log('[audioHelper] ⚠️ URL đã có trong queue, bỏ qua.');
                return;
            }

            if (_isPlaying) {
                _queue.push(url);
                console.log(`[audioHelper] ➕ Thêm vào queue: ${url} | Queue size: ${_queue.length}`);
            } else {
                _queue.unshift(url);
                await _playNext();
            }
        },

        /**
         * Dừng hoàn toàn và xóa queue.
         */
        stop: function () {
            _queue = [];
            _isPlaying = false;
            if (_audio && !_audio.paused) {
                _audio.pause();
                _audio.currentTime = 0;
            }
            console.log('[audioHelper] ⏹️ Dừng & xóa queue');
        },

        /**
         * Bỏ qua bài hiện tại → phát bài tiếp theo.
         */
        skip: function () {
            _isPlaying = false;
            if (_audio && !_audio.paused) {
                _audio.pause();
                _audio.currentTime = 0;
            }
            console.log('[audioHelper] ⏭️ Skip → phát bài tiếp');
            _playNext();
        },

        /**
         * Phát tại mốc thời gian cụ thể (dùng khi đổi ngôn ngữ).
         * Clear queue trước để phát ngay.
         */
        playAtTime: async function (url, startTime) {
            _queue = [];
            _isPlaying = false;

            const audio = _getOrCreate();
            audio.src = url;
            audio.currentTime = startTime;
            audio.muted = false;
            _isPlaying = true;

            try {
                await audio.play();
                console.log('[audioHelper] playAtTime:', url, '@', startTime, 's');
            } catch (err) {
                _isPlaying = false;
                console.error('[audioHelper] playAtTime lỗi:', err);
            }
        },

        /**
         * Tạm dừng (giữ nguyên vị trí).
         */
        pause: function () {
            if (_audio) {
                _audio.pause();
                _isPlaying = false;
            }
        },

        /**
         * Tiếp tục từ vị trí đang dừng.
         */
        resume: function () {
            if (_audio && _audio.src) {
                _isPlaying = true;
                _audio.play().catch(e => {
                    _isPlaying = false;
                    console.error('[audioHelper] Resume lỗi:', e);
                });
            }
        },

        /**
         * Đang phát không?
         */
        isPlaying: function () {
            return _isPlaying;
        },

        /**
         * Số bài còn trong queue.
         */
        getQueueLength: function () {
            return _queue.length;
        },

        /**
         * Xem toàn bộ queue (debug).
         */
        getQueue: function () {
            return [..._queue];
        },

        /**
         * Phát theo element ID (dùng cho PoiDetail player).
         */
        playById: function (elementId) {
            const el = document.getElementById(elementId);
            if (el) el.play().catch(e => console.error('[audioHelper] playById lỗi:', e));
        },

        /**
         * Pause theo element ID.
         */
        pauseById: function (elementId) {
            const el = document.getElementById(elementId);
            if (el) el.pause();
        },

        /**
         * Vị trí thời gian hiện tại (giây).
         */
        getCurrentTime: function () {
            return _audio ? _audio.currentTime : 0;
        },

        setDotNetReference: setDotNetReference
    };
})();
