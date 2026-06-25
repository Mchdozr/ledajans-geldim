window.ledajansGeo = {
    getCurrentPosition: function (maxAccuracyMeters) {
        maxAccuracyMeters = maxAccuracyMeters || 30;
        const waitMs = 25000;

        return new Promise((resolve, reject) => {
            if (!navigator.geolocation) {
                reject("Tarayıcınız konum servisini desteklemiyor.");
                return;
            }

            let best = null;
            let watchId = null;
            let settled = false;

            const finish = (fn) => {
                if (settled) return;
                settled = true;
                clearTimeout(timer);
                if (watchId !== null) navigator.geolocation.clearWatch(watchId);
                fn();
            };

            const onError = (err) => {
                let msg = "Konum alınamadı.";
                if (err.code === 1) msg = "Konum izni reddedildi. Lütfen tarayıcı ayarlarından izin verin.";
                else if (err.code === 2) msg = "Konum bilgisi şu an kullanılamıyor. GPS açık ve açık alanda olun.";
                else if (err.code === 3) msg = "Konum isteği zaman aşımına uğradı. Tekrar deneyin.";
                finish(() => reject(msg));
            };

            const tryResolve = (pos) => {
                const candidate = {
                    latitude: pos.coords.latitude,
                    longitude: pos.coords.longitude,
                    accuracy: pos.coords.accuracy
                };
                if (!best || candidate.accuracy < best.accuracy)
                    best = candidate;
                if (candidate.accuracy <= maxAccuracyMeters)
                    finish(() => resolve(candidate));
            };

            const timer = setTimeout(() => {
                if (best && best.accuracy <= maxAccuracyMeters)
                    finish(() => resolve(best));
                else if (best)
                    finish(() => reject(`Konum hassasiyeti düşük (${Math.round(best.accuracy)} m). Açık alana çıkıp tekrar deneyin.`));
                else
                    finish(() => reject("Konum alınamadı. GPS açık ve açık alanda tekrar deneyin."));
            }, waitMs);

            watchId = navigator.geolocation.watchPosition(tryResolve, onError, {
                enableHighAccuracy: true,
                maximumAge: 0,
                timeout: waitMs
            });
        });
    }
};

window.ledajansLocationMap = {
    _maps: {},

    init: function (elementId, officeLat, officeLng, radius) {
        const el = document.getElementById(elementId);
        if (!el) return;

        this.destroy(elementId);

        const map = L.map(elementId, { zoomControl: true, attributionControl: true }).setView([officeLat, officeLng], 16);
        L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
            maxZoom: 19,
            attribution: '© OpenStreetMap'
        }).addTo(map);

        const officeCircle = L.circle([officeLat, officeLng], {
            radius: radius,
            color: '#f46f2c',
            fillColor: '#f46f2c',
            fillOpacity: 0.14,
            weight: 2
        }).addTo(map);

        L.marker([officeLat, officeLng], {
            interactive: false,
            keyboard: false
        }).addTo(map);

        this._maps[elementId] = { map, officeCircle, officeLat, officeLng, radius, userMarker: null, userRing: null };
        setTimeout(() => map.invalidateSize(), 250);
    },

    updateUser: function (elementId, lat, lng, accuracy, withinRadius) {
        const inst = this._maps[elementId];
        if (!inst) return;

        const color = withinRadius ? '#27ae60' : '#e74c3c';
        const pos = [lat, lng];

        if (!inst.userMarker) {
            inst.userMarker = L.circleMarker(pos, {
                radius: 9,
                color: '#fff',
                fillColor: color,
                fillOpacity: 1,
                weight: 3
            }).addTo(inst.map).bindTooltip('Konumunuz', { permanent: false, direction: 'top' });
        } else {
            inst.userMarker.setLatLng(pos);
            inst.userMarker.setStyle({ fillColor: color });
        }

        const acc = Math.max(accuracy || 0, 8);
        if (!inst.userRing) {
            inst.userRing = L.circle(pos, {
                radius: acc,
                color: color,
                fillColor: color,
                fillOpacity: 0.1,
                weight: 1,
                dashArray: '4 4'
            }).addTo(inst.map);
        } else {
            inst.userRing.setLatLng(pos);
            inst.userRing.setRadius(acc);
            inst.userRing.setStyle({ color: color, fillColor: color });
        }

        const bounds = L.latLngBounds([
            [inst.officeLat, inst.officeLng],
            pos
        ]);
        inst.map.fitBounds(bounds.pad(0.35), { maxZoom: 17 });
    },

    destroy: function (elementId) {
        const inst = this._maps[elementId];
        if (!inst) return;
        inst.map.remove();
        delete this._maps[elementId];
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
