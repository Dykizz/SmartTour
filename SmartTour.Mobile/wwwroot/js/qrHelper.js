/**
 * qrHelper.js
 * - Dùng camera + BarcodeDetector để đọc QR.
 * - Gửi kết quả về C# qua DotNetObjectReference (method: OnQrCodeDetected).
 */
window.qrHelper = (() => {
    let _dotNetRef = null;
    let _stream = null;
    let _video = null;
    let _detector = null;
    let _running = false;
    let _rafId = null;

    function setDotNetReference(dotNetRef) {
        _dotNetRef = dotNetRef;
    }

    function _clearContainer(container) {
        while (container.firstChild) container.removeChild(container.firstChild);
    }

    async function stopScan() {
        _running = false;
        if (_rafId) {
            cancelAnimationFrame(_rafId);
            _rafId = null;
        }

        try {
            if (_video) {
                _video.pause();
            }
        } catch { }

        if (_stream) {
            try {
                _stream.getTracks().forEach(t => t.stop());
            } catch { }
            _stream = null;
        }

        if (_video && _video.parentElement) {
            try {
                _video.parentElement.removeChild(_video);
            } catch { }
        }
        _video = null;
        _detector = null;
    }

    async function startScan(containerId) {
        if (_running) return true;

        if (typeof window.BarcodeDetector === "undefined") {
            // Không hỗ trợ quét tự động
            return false;
        }

        const container = document.getElementById(containerId);
        if (!container) return false;

        _clearContainer(container);

        const video = document.createElement("video");
        video.setAttribute("playsinline", true);
        video.muted = true;
        video.autoplay = true;
        container.appendChild(video);
        _video = video;

        try {
            // Yêu cầu camera
            _stream = await navigator.mediaDevices.getUserMedia({
                video: { facingMode: { ideal: "environment" } },
                audio: false
            });
        } catch (e) {
            console.error("[qrHelper] getUserMedia failed:", e);
            await stopScan();
            return false;
        }

        try {
            video.srcObject = _stream;
            await video.play();
        } catch (e) {
            console.error("[qrHelper] video.play failed:", e);
            await stopScan();
            return false;
        }

        _detector = new window.BarcodeDetector({ formats: ["qr_code"] });
        _running = true;

        const scanLoop = async () => {
            if (!_running || !_video) return;

            try {
                const barcodes = await _detector.detect(_video);
                if (barcodes && barcodes.length > 0) {
                    const rawValue = barcodes[0].rawValue || "";
                    _running = false;

                    // Gọi về C# rồi stop camera
                    if (_dotNetRef) {
                        try {
                            await _dotNetRef.invokeMethodAsync("OnQrCodeDetected", rawValue);
                        } catch { }
                    }
                    await stopScan();
                    return;
                }
            } catch (e) {
                // detect có thể lỗi khi frame chưa sẵn sàng
                // bỏ qua và thử lại ở frame tiếp theo
            }

            _rafId = requestAnimationFrame(scanLoop);
        };

        _rafId = requestAnimationFrame(scanLoop);
        return true;
    }

    return {
        setDotNetReference,
        startScan,
        stopScan
    };
})();

