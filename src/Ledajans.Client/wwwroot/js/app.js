window.ledajansGeo = {
    /**
     * @param {object} options
     * @param {string} options.mode - 'preview' | 'checkin'
     * @param {number} options.idealAccuracyMeters
     * @param {number} options.maxAccuracyMeters
     * @param {number} options.timeoutMs
     */
    getCurrentPosition: function (options) {
        if (typeof options === 'number') {
            options = { maxAccuracyMeters: options };
        }
        options = options || {};

        const isPreview = options.mode === 'preview';
        const isMobile = /Android|iPhone|iPad|iPod|Mobile|webOS|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent);
        const isDesktopCheckin = !isPreview && !isMobile;

        const idealAccuracy = options.idealAccuracyMeters ?? (isPreview ? 60 : (isDesktopCheckin ? 80 : 40));
        const maxAccuracy = options.maxAccuracyMeters ?? (isPreview ? 250 : (isDesktopCheckin ? 250 : 120));
        const timeoutMs = options.timeoutMs ?? (isPreview ? 10000 : (isDesktopCheckin ? 60000 : 45000));
        const earlyAcceptMs = isPreview ? 3500 : (isDesktopCheckin ? 18000 : 6000);
        const earlyAcceptMaxAccuracy = isDesktopCheckin ? 220 : 90;
        const geoOptions = {
            enableHighAccuracy: true,
            maximumAge: isPreview ? 0 : (isDesktopCheckin ? 0 : 30000),
            timeout: isPreview ? Math.min(timeoutMs, 15000) : timeoutMs
        };

        return new Promise((resolve, reject) => {
            if (!navigator.geolocation) {
                reject("Tarayıcınız konum servisini desteklemiyor.");
                return;
            }

            let best = null;
            let watchId = null;
            let settled = false;
            const readings = [];

            const toResult = (p) => ({
                latitude: p.latitude,
                longitude: p.longitude,
                accuracy: p.accuracy,
                lowAccuracy: p.accuracy > idealAccuracy
            });

            const earlyTimer = setTimeout(() => {
                if (best && best.accuracy <= earlyAcceptMaxAccuracy) finish(() => resolve(toResult(best)));
            }, earlyAcceptMs);

            const timer = setTimeout(() => {
                if (!best) {
                    finish(() => reject("Konum alınamadı. GPS açık ve açık alanda tekrar deneyin."));
                    return;
                }
                finish(() => resolve(toResult(best)));
            }, timeoutMs);

            const finish = (fn) => {
                if (settled) return;
                settled = true;
                clearTimeout(timer);
                clearTimeout(earlyTimer);
                if (watchId !== null) navigator.geolocation.clearWatch(watchId);
                fn();
            };

            const onError = (err) => {
                if (best) {
                    finish(() => resolve(toResult(best)));
                    return;
                }
                let msg = "Konum alınamadı.";
                if (err.code === 1) msg = "Konum izni reddedildi. Lütfen tarayıcı ayarlarından izin verin.";
                else if (err.code === 2) msg = "Konum bilgisi şu an kullanılamıyor. GPS açık ve açık alanda olun.";
                else if (err.code === 3 && best && best.accuracy <= maxAccuracy) {
                    finish(() => resolve(toResult(best)));
                    return;
                } else if (err.code === 3) {
                    msg = "Konum isteği zaman aşımına uğradı. Tekrar deneyin.";
                }
                finish(() => reject(msg));
            };

            const isStable = () => {
                if (readings.length < 2) return false;
                const recent = readings.slice(-3);
                const accSpread = Math.max(...recent.map(r => r.accuracy)) - Math.min(...recent.map(r => r.accuracy));
                const last = recent[recent.length - 1];
                return last.accuracy <= idealAccuracy && accSpread <= 20;
            };

            const consider = (pos) => {
                const candidate = {
                    latitude: pos.coords.latitude,
                    longitude: pos.coords.longitude,
                    accuracy: pos.coords.accuracy
                };
                if (!Number.isFinite(candidate.latitude) || !Number.isFinite(candidate.longitude)) return;

                readings.push(candidate);
                if (!best || candidate.accuracy < best.accuracy) best = candidate;

                if (candidate.accuracy <= idealAccuracy && isStable()) {
                    finish(() => resolve(toResult(best)));
                }
            };

            navigator.geolocation.getCurrentPosition(consider, () => { /* watch devam */ }, geoOptions);

            watchId = navigator.geolocation.watchPosition(consider, onError, geoOptions);
        });
    }
};

window.ledajansMap = {
    _map: null,
    _marker: null,
    _circle: null,
    _ref: null,

    init: function (elementId, lat, lng, radius, dotNetRef) {
        this._ref = dotNetRef;
        const el = document.getElementById(elementId);
        if (!el) return;

        if (this._map) {
            this._map.remove();
            this._map = null;
        }

        this._map = L.map(elementId).setView([lat, lng], 16);
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            maxZoom: 19,
            attribution: '© OpenStreetMap'
        }).addTo(this._map);

        this._marker = L.marker([lat, lng], { draggable: true }).addTo(this._map);
        this._circle = L.circle([lat, lng], { radius: radius, color: '#f46f2c', fillColor: '#f46f2c', fillOpacity: 0.12 }).addTo(this._map);

        const self = this;
        this._map.on('click', function (e) {
            self._setPoint(e.latlng.lat, e.latlng.lng);
        });
        this._marker.on('dragend', function (e) {
            const p = e.target.getLatLng();
            self._setPoint(p.lat, p.lng);
        });

        setTimeout(() => self._map.invalidateSize(), 200);
    },

    _setPoint: function (lat, lng) {
        this._marker.setLatLng([lat, lng]);
        this._circle.setLatLng([lat, lng]);
        if (this._ref) this._ref.invokeMethodAsync('OnMapClick', lat, lng);
    },

    setView: function (lat, lng, radius) {
        if (!this._map || !this._marker || !this._circle) return;
        const parsedLat = Number(lat);
        const parsedLng = Number(lng);
        if (!Number.isFinite(parsedLat) || !Number.isFinite(parsedLng)) return;
        this._marker.setLatLng([parsedLat, parsedLng]);
        this._circle.setLatLng([parsedLat, parsedLng]);
        this._circle.setRadius(radius);
        this._map.setView([parsedLat, parsedLng], this._map.getZoom() || 16);
    },

    updateRadius: function (radius) {
        if (this._circle) this._circle.setRadius(radius);
    }
};

window.ledajansDownload = function (fileName, contentType, base64) {
    const link = document.createElement('a');
    link.href = `data:${contentType};base64,${base64}`;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};
