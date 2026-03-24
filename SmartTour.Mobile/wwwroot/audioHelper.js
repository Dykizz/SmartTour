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
            _audio.id = 'geofence-audio-player';
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

    // ── Unlock autoplay (Android WebView / iOS Safari) ────────────────────────────────
    function _unlockAudio() {
        // Phải unlock chính gốc cái thẻ Audio mà ta xài, chứ không unlock thẻ ảo!
        const a = _getOrCreate();
        if (a.src && a.src !== window.location.href && a.src !== "") {
            // Đang có source thật thì không đè
            return;
        }

        a.src = 'data:audio/wav;base64,UklGRiQAAABXQVZFZm10IBAAAAABAAEARKwAAIhYAQACABAAZGF0YQAAAAA=';
        a.muted = true;
        
        let playPromise = a.play();
        if (playPromise !== undefined) {
            playPromise.then(() => {
                a.pause();
                console.log('[audioHelper] 🔓 Đã Unlock Muted Audio thành công trên Device.');
            }).catch(() => { });
        }
    }
    
    // Gắn móc bám dính vào toàn bộ các thao tác vuốt / lướt màn hình để mở khóa Loa ngay tức khắc.
    document.addEventListener('touchstart', _unlockAudio, { once: true, passive: true });
    document.addEventListener('click', _unlockAudio, { once: true, capture: true });
    document.addEventListener('touchend', _unlockAudio, { once: true, passive: true });

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
            console.log('[audioHelper] ⏹️ Dừng toàn bộ hệ thống audio...');
            _queue = [];
            _isPlaying = false;

            // 1. Dừng audio singleton
            if (_audio) {
                _audio.pause();
                _audio.src = ""; // Clear src to be sure
                _audio.load();
                _audio.currentTime = 0;
            }

            // 2. Dừng tất cả thẻ <audio> khác trên toàn trang (phòng hờ)
            try {
                const allAudios = document.querySelectorAll('audio');
                allAudios.forEach(a => {
                    a.pause();
                    a.currentTime = 0;
                });
            } catch (e) {
                console.error('[audioHelper] Lỗi khi dừng tất cả audio:', e);
            }
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
            console.log(`[audioHelper] ⚡ playAtTime gọi: ${url} @ ${startTime}s`);
            _queue = [];

            const audio = _getOrCreate();

            // Nếu cùng URL, chỉ cần đưa về đầu và phát (giảm hiện tượng chớp/mất tiếng)
            if (audio.src.includes(url) || url.includes(audio.src)) {
                audio.currentTime = startTime;
                _isPlaying = true;
                try {
                    await audio.play();
                } catch (e) {
                    console.error('[audioHelper] playAtTime (same src) lỗi:', e);
                    _isPlaying = false;
                }
                return;
            }

            // Nếu đổi URL: dừng cái cũ trước
            _isPlaying = false;
            audio.pause();
            audio.src = url;
            audio.load(); // Đảm bảo nạp lại
            audio.currentTime = startTime;
            audio.muted = false;
            _isPlaying = true;

            try {
                let playProm = audio.play();
                if (playProm !== undefined) {
                    await playProm;
                }
                console.log('[audioHelper] playAtTime thành công:', url);
            } catch (err) {
                // Fallback cứu hộ: Lỡ khách hàng quét QR mà chả có tí ngón tay nào chạm màn hình
                // Bị Chrome chặn cứng thì đánh lừa nó bằng Muted trước để chạy, sau đó mở Volume lên lại.
                if (err.name === 'NotAllowedError') {
                    console.warn('[audioHelper] ⚠️ Bị chặn Autoplay! Bật chế độ Muted vạch đường...');
                    audio.muted = true;
                    try {
                        await audio.play();
                        setTimeout(() => { audio.muted = false; }, 400); // 0.4s sau bùi tiếng lại
                        console.log('[audioHelper] Đã Cứu Hộ mở khóa mồm bằng Muted Trick!');
                    } catch (e2) {
                        _isPlaying = false;
                        console.error('[audioHelper] Muted Try 2 thất bại:', e2);
                    }
                } else {
                    _isPlaying = false;
                    console.error('[audioHelper] playAtTime (new src) lỗi:', err);
                }
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
