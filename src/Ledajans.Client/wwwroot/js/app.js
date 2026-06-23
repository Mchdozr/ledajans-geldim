window.ledajansGeo = {
    getCurrentPosition: function () {
        return new Promise((resolve, reject) => {
            if (!navigator.geolocation) {
                reject("Tarayıcınız konum servisini desteklemiyor.");
                return;
            }
            navigator.geolocation.getCurrentPosition(
                pos => resolve({
                    latitude: pos.coords.latitude,
                    longitude: pos.coords.longitude,
                    accuracy: pos.coords.accuracy
                }),
                err => {
                    let msg = "Konum alınamadı.";
                    if (err.code === 1) msg = "Konum izni reddedildi. Lütfen tarayıcı ayarlarından izin verin.";
                    else if (err.code === 2) msg = "Konum bilgisi şu an kullanılamıyor.";
                    else if (err.code === 3) msg = "Konum isteği zaman aşımına uğradı.";
                    reject(msg);
                },
                { enableHighAccuracy: true, timeout: 15000, maximumAge: 0 }
            );
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
