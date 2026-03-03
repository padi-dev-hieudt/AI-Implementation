/* ═══════════════════════════════════════════════════════════════
   wwwroot/Services/ApiService.js
   Base HTTP wrapper. All server responses conform to ApiResponse<T>:
     { success, message, data, errors }
   Throws Error with a user-visible message on non-success.
   ════════════════════════════════════════════════════════════════ */

'use strict';

const ApiService = (() => {
    async function request(method, url, body) {
        const opts = {
            method,
            headers: { 'Content-Type': 'application/json' },
            credentials: 'same-origin'
        };
        if (body !== undefined) opts.body = JSON.stringify(body);

        let res;
        try {
            res = await fetch(url, opts);
        } catch {
            throw new Error('Không thể kết nối đến máy chủ.');
        }

        let json;
        try {
            json = await res.json();
        } catch {
            throw new Error(`Lỗi máy chủ (${res.status}).`);
        }

        if (!json.success) {
            const msg = (json.errors && json.errors.length)
                ? json.errors.join('; ')
                : (json.message || 'Yêu cầu thất bại.');
            throw new Error(msg);
        }

        return json;
    }

    return {
        get:  (url)        => request('GET',    url),
        post: (url, body)  => request('POST',   url, body),
        put:  (url, body)  => request('PUT',    url, body),
        del:  (url)        => request('DELETE', url)
    };
})();
