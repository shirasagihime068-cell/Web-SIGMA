(function () {
    function getBaseUrl() {
        return (window.API_BASE_URL || '').replace(/\/$/, '');
    }

    function makeUrl(endpoint) {
        if (/^https?:\/\//i.test(endpoint)) return endpoint;
        return `${getBaseUrl()}${endpoint.startsWith('/') ? endpoint : `/${endpoint}`}`;
    }

    async function parseResponse(response) {
        const contentType = response.headers.get('content-type') || '';
        if (contentType.includes('application/json')) {
            return await response.json();
        }
        return await response.text();
    }

    async function request(endpoint, options = {}) {
        const response = await fetch(makeUrl(endpoint), {
            credentials: 'include',
            ...options,
            headers: {
                'Accept': 'application/json',
                ...(options.body instanceof FormData ? {} : { 'Content-Type': 'application/json' }),
                ...(options.headers || {})
            }
        });

        if (response.status === 401) {
            if (window.location.pathname !== '/' && !window.location.pathname.toLowerCase().includes('/auth/login')) {
                window.location.href = '/Auth/Login';
            }
        }

        const data = await parseResponse(response);
        return { response, data };
    }

    window.sigmaApi = {
        get: endpoint => request(endpoint, { method: 'GET' }),
        postJson: (endpoint, payload) => request(endpoint, { method: 'POST', body: JSON.stringify(payload) }),
        putJson: (endpoint, payload) => request(endpoint, { method: 'PUT', body: JSON.stringify(payload) }),
        delete: endpoint => request(endpoint, { method: 'DELETE' }),
        postForm: (endpoint, formData) => request(endpoint, { method: 'POST', body: formData })
    };
})();
