// Backward-compatible global API helper used by legacy pages.
window.api = {
    async postJson(endpoint, data) {
        try {
            const response = await fetch(`${window.API_BASE_URL}${endpoint}`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Accept': 'application/json'
                },
                body: JSON.stringify(data),
                credentials: 'include'
            });

            const contentType = response.headers.get('content-type') || '';
            const result = contentType.includes('application/json')
                ? await response.json()
                : { success: response.ok, message: await response.text() };

            return result;
        } catch (error) {
            console.error('API Error:', error);
            return { success: false, message: 'Tidak dapat terhubung ke server backend.' };
        }
    },

    async get(endpoint) {
        try {
            const response = await fetch(`${window.API_BASE_URL}${endpoint}`, {
                method: 'GET',
                headers: { 'Accept': 'application/json' },
                credentials: 'include'
            });

            const contentType = response.headers.get('content-type') || '';
            return contentType.includes('application/json')
                ? await response.json()
                : { success: response.ok, message: await response.text() };
        } catch (error) {
            console.error('API Error:', error);
            return { success: false, message: 'Terjadi kesalahan pada jaringan.' };
        }
    }
};
