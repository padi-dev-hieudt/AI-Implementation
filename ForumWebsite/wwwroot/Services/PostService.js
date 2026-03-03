/* ═══════════════════════════════════════════════════════════════
   wwwroot/Services/PostService.js
   Named wrappers for /api/post/* endpoints.
   Depends on: ApiService
   ════════════════════════════════════════════════════════════════ */

'use strict';

const PostService = (() => {
    return {
        getAll:    (page, pageSize) =>
            ApiService.get(`/api/post?page=${page}&pageSize=${pageSize}`),

        getById:   (id) =>
            ApiService.get(`/api/post/${id}`),

        getByUser: (userId, page, pageSize) =>
            ApiService.get(`/api/post/user/${userId}?page=${page}&pageSize=${pageSize}`),

        create:    (dto)     => ApiService.post('/api/post', dto),
        update:    (id, dto) => ApiService.put(`/api/post/${id}`, dto),
        remove:    (id)      => ApiService.del(`/api/post/${id}`),
        close:     (id)      => ApiService.put(`/api/post/${id}/close`)
    };
})();
