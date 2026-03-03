/* ═══════════════════════════════════════════════════════════════
   wwwroot/js/utils.js
   Pure helper functions and shared Quill toolbar config.
   No dependencies — safe to load first.
   ════════════════════════════════════════════════════════════════ */

'use strict';

/* ── Utils ───────────────────────────────────────────────────────
   Pure helper functions. No side-effects except flash().
   ─────────────────────────────────────────────────────────────── */
const Utils = (() => {
    const PALETTE = [
        '#2563eb','#7c3aed','#db2777','#dc2626',
        '#ea580c','#d97706','#16a34a','#0891b2'
    ];

    function timeAgo(dateStr) {
        const diff = (Date.now() - new Date(dateStr).getTime()) / 1000;
        if (diff < 60)       return 'vừa xong';
        if (diff < 3600)     return `${Math.floor(diff / 60)} phút trước`;
        if (diff < 86400)    return `${Math.floor(diff / 3600)} giờ trước`;
        if (diff < 2592000)  return `${Math.floor(diff / 86400)} ngày trước`;
        if (diff < 31536000) return `${Math.floor(diff / 2592000)} tháng trước`;
        return `${Math.floor(diff / 31536000)} năm trước`;
    }

    function strToColor(str) {
        let hash = 0;
        for (let i = 0; i < str.length; i++) hash = str.charCodeAt(i) + ((hash << 5) - hash);
        return PALETTE[Math.abs(hash) % PALETTE.length];
    }

    function avatarHtml(username, extraClass) {
        const cls     = extraClass ? ` ${extraClass}` : '';
        const initial = (username || '?')[0].toUpperCase();
        const bg      = strToColor(username || '?');
        return `<div class="user-avatar${cls}" style="background:${bg}">${initial}</div>`;
    }

    function escapeHtml(s) {
        return String(s)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    function fmtNum(n) {
        if (n >= 1000) return (n / 1000).toFixed(1).replace(/\.0$/, '') + 'k';
        return String(n);
    }

    const _vnMap = {
        'à':'a','á':'a','ả':'a','ã':'a','ạ':'a',
        'ă':'a','ắ':'a','ặ':'a','ằ':'a','ẳ':'a','ẵ':'a',
        'â':'a','ấ':'a','ầ':'a','ẩ':'a','ẫ':'a','ậ':'a',
        'è':'e','é':'e','ẻ':'e','ẽ':'e','ẹ':'e',
        'ê':'e','ế':'e','ề':'e','ể':'e','ễ':'e','ệ':'e',
        'ì':'i','í':'i','ỉ':'i','ĩ':'i','ị':'i',
        'ò':'o','ó':'o','ỏ':'o','õ':'o','ọ':'o',
        'ô':'o','ố':'o','ồ':'o','ổ':'o','ỗ':'o','ộ':'o',
        'ơ':'o','ớ':'o','ờ':'o','ở':'o','ỡ':'o','ợ':'o',
        'ù':'u','ú':'u','ủ':'u','ũ':'u','ụ':'u',
        'ư':'u','ứ':'u','ừ':'u','ử':'u','ữ':'u','ự':'u',
        'ỳ':'y','ý':'y','ỷ':'y','ỹ':'y','ỵ':'y',
        'đ':'d'
    };

    function slugify(text) {
        return String(text)
            .toLowerCase()
            .replace(/[^\u0000-\u007E]/g, c => _vnMap[c] || '')
            .replace(/[^a-z0-9\s-]/g, '')
            .trim()
            .replace(/\s+/g, '-')
            .replace(/-+/g, '-')
            .substring(0, 80);
    }

    function flash(message, type) {
        const container = document.getElementById('flashContainer');
        const msgEl     = document.getElementById('flashMsg');
        const textEl    = document.getElementById('flashText');
        if (!container || !msgEl || !textEl) return;

        textEl.textContent = message;
        msgEl.className    = `alert alert-${type || 'success'} alert-dismissible fade show mb-0`;
        container.style.display = 'block';

        const tid = setTimeout(() => {
            const instance = bootstrap.Alert.getOrCreateInstance(msgEl);
            instance && instance.close();
            container.style.display = 'none';
        }, 4000);

        msgEl.addEventListener('closed.bs.alert', () => clearTimeout(tid), { once: true });
    }

    return { timeAgo, strToColor, avatarHtml, escapeHtml, fmtNum, flash, slugify };
})();

/* ── Quill toolbar config ────────────────────────────────────────
   Shared between post-detail.js and create-post.js.
   _quillToolbar() is a plain function (not inside the IIFE) so both
   page scripts can call it after Quill is loaded via @section Scripts.
   ─────────────────────────────────────────────────────────────── */
function _quillToolbar() {
    return [
        [{ header: [2, 3, false] }],
        ['bold', 'italic', 'underline', 'strike'],
        [{ list: 'ordered' }, { list: 'bullet' }],
        ['blockquote', 'code-block'],
        ['link'],
        ['clean']
    ];
}
