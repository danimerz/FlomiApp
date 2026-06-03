window.qrScanner = (function () {
    let stream = null;
    let animFrame = null;
    let dotnetRef = null;

    return {
        start: async function (videoId, canvasId, dotnetHelper) {
            dotnetRef = dotnetHelper;
            const video  = document.getElementById(videoId);
            const canvas = document.getElementById(canvasId);
            if (!video || !canvas) return;

            try {
                stream = await navigator.mediaDevices.getUserMedia({
                    video: { facingMode: 'environment' }
                });
                video.srcObject = stream;
                video.setAttribute('playsinline', true);
                await video.play();
                tick(video, canvas);
            } catch (e) {
                console.error('Camera error:', e);
                dotnetRef.invokeMethodAsync('OnScanError', e.message);
            }
        },

        stop: function () {
            if (animFrame) { cancelAnimationFrame(animFrame); animFrame = null; }
            if (stream)    { stream.getTracks().forEach(t => t.stop()); stream = null; }
        }
    };

    function tick(video, canvas) {
        if (video.readyState === video.HAVE_ENOUGH_DATA) {
            const ctx = canvas.getContext('2d');
            canvas.height = video.videoHeight;
            canvas.width  = video.videoWidth;
            ctx.drawImage(video, 0, 0, canvas.width, canvas.height);
            const imageData = ctx.getImageData(0, 0, canvas.width, canvas.height);
            const code = jsQR(imageData.data, imageData.width, imageData.height, {
                inversionAttempts: 'dontInvert'
            });
            if (code && code.data) {
                dotnetRef.invokeMethodAsync('OnQrDetected', code.data);
                return; // Pause after detection, C# resumes if needed
            }
        }
        animFrame = requestAnimationFrame(() => tick(video, canvas));
    }
})();
