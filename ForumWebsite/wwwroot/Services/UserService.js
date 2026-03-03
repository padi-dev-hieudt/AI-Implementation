/* ═══════════════════════════════════════════════════════════════
   wwwroot/Services/UserService.js
   Named wrappers for /api/user/* endpoints.
   Depends on: ApiService
   ════════════════════════════════════════════════════════════════ */

'use strict';

const UserService = (() => {
    return {
        me:         ()    => ApiService.get('/api/user/me'),
        login:      (dto) => ApiService.post('/api/user/login', dto),
        register:   (dto) => ApiService.post('/api/user/register', dto),
        logout:     ()    => ApiService.post('/api/user/logout'),
        getProfile: (id)  => ApiService.get(`/api/user/profile/${id}`)
    };
})();
