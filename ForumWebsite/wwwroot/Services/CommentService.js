/* ═══════════════════════════════════════════════════════════════
   wwwroot/Services/CommentService.js
   Named wrappers for /api/comment/* endpoints.
   Depends on: ApiService
   ════════════════════════════════════════════════════════════════ */

'use strict';

const CommentService = (() => {
    return {
        create: (dto)       => ApiService.post('/api/comment', dto),
        update: (id, dto)   => ApiService.put(`/api/comment/${id}`, dto),
        remove: (id)        => ApiService.del(`/api/comment/${id}`)
    };
})();
