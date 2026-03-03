/* ═══════════════════════════════════════════════════════════════
   forum.js  —  Forum client-side JavaScript
   All modules are IIFEs; DOMContentLoaded bootstraps everything.
   ════════════════════════════════════════════════════════════════ */

'use strict';

/* ── Api ─────────────────────────────────────────────────────────
   Thin fetch wrapper.  Every response must be ApiResponse<T>:
     { success, message, data, errors }
   Throws Error with user-visible message on failure.
   ─────────────────────────────────────────────────────────────── */
const Api = (() => {
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

/* ── Utils ───────────────────────────────────────────────────────
   Pure helper functions.  No side-effects except flash().
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

        // Allow manual dismiss to clear the timer
        msgEl.addEventListener('closed.bs.alert', () => clearTimeout(tid), { once: true });
    }

    return { timeAgo, strToColor, avatarHtml, escapeHtml, fmtNum, flash, slugify };
})();

/* ── Auth ────────────────────────────────────────────────────────
   Manages authentication state and the login / register forms.
   Auth.user is null for guests, UserProfileDto object when logged in.
   ─────────────────────────────────────────────────────────────── */
const Auth = (() => {
    let _user = null;

    /* Fetch current user from /api/user/me (HttpOnly cookie auth) */
    async function init() {
        try {
            const res = await Api.get('/api/user/me');
            _user = res.data;
        } catch {
            _user = null;
        }
        _updateNav();
    }

    function _updateNav() {
        const guestNav = document.getElementById('guestNav');
        const userNav  = document.getElementById('userNav');
        if (!guestNav || !userNav) return;

        if (_user) {
            guestNav.classList.add('d-none');
            userNav.classList.remove('d-none');

            const initial = (_user.username || '?')[0].toUpperCase();
            const bg      = Utils.strToColor(_user.username || '');

            const navAvatar  = document.getElementById('navAvatar');
            const navUsername = document.getElementById('navUsername');
            const ddUsername  = document.getElementById('ddUsername');
            const ddEmail     = document.getElementById('ddEmail');
            const ddRole      = document.getElementById('ddRole');

            if (navAvatar) {
                navAvatar.textContent   = initial;
                navAvatar.style.background = bg;
            }
            if (navUsername) navUsername.textContent = _user.username;
            if (ddUsername)  ddUsername.textContent  = _user.username;
            if (ddEmail)     ddEmail.textContent     = _user.email;
            if (ddRole) {
                ddRole.textContent = _user.role;
                ddRole.className   = `badge mt-1 badge-role-${(_user.role || 'user').toLowerCase()}`;
            }
            const ddProfileLink = document.getElementById('ddProfileLink');
            if (ddProfileLink && _user.id) ddProfileLink.href = `/profile/${_user.id}`;
        } else {
            guestNav.classList.remove('d-none');
            userNav.classList.add('d-none');
        }

        /* Intercept "New post" button for guests */
        const btnNew = document.getElementById('btnNewPost');
        if (btnNew) {
            btnNew.addEventListener('click', e => {
                if (!_user) {
                    e.preventDefault();
                    const modal = document.getElementById('loginModal');
                    modal && bootstrap.Modal.getOrCreateInstance(modal).show();
                }
            });
        }
    }

    function setupLoginForm() {
        const form    = document.getElementById('loginForm');
        if (!form) return;

        form.addEventListener('submit', async e => {
            e.preventDefault();
            const email    = document.getElementById('loginEmail')?.value.trim();
            const password = document.getElementById('loginPassword')?.value;
            const errEl    = document.getElementById('loginError');
            const spinner  = document.getElementById('loginSpinner');
            const btn      = document.getElementById('btnLogin');
            if (!email || !password) return;

            try {
                errEl?.classList.add('d-none');
                spinner?.classList.remove('d-none');
                if (btn) btn.disabled = true;

                const res = await Api.post('/api/user/login', { email, password });
                _user = res.data;   // AuthResponseDto (username, role, etc.)
                _updateNav();
                bootstrap.Modal.getOrCreateInstance(document.getElementById('loginModal'))?.hide();
                Utils.flash(`Chào mừng trở lại, ${_user.username}!`, 'success');
                form.reset();
                setTimeout(() => location.reload(), 800);
            } catch (err) {
                if (errEl) { errEl.textContent = err.message; errEl.classList.remove('d-none'); }
            } finally {
                spinner?.classList.add('d-none');
                if (btn) btn.disabled = false;
            }
        });
    }

    function setupRegisterForm() {
        const form = document.getElementById('registerForm');
        if (!form) return;

        form.addEventListener('submit', async e => {
            e.preventDefault();
            const username        = document.getElementById('regUsername')?.value.trim();
            const email           = document.getElementById('regEmail')?.value.trim();
            const password        = document.getElementById('regPassword')?.value;
            const confirmPassword = document.getElementById('regConfirmPassword')?.value;
            const errEl           = document.getElementById('registerError');
            const spinner         = document.getElementById('registerSpinner');
            const btn             = document.getElementById('btnRegister');
            if (!username || !email || !password || !confirmPassword) return;

            try {
                errEl?.classList.add('d-none');
                spinner?.classList.remove('d-none');
                if (btn) btn.disabled = true;

                const res = await Api.post('/api/user/register',
                    { username, email, password, confirmPassword });
                _user = res.data;
                _updateNav();
                bootstrap.Modal.getOrCreateInstance(document.getElementById('registerModal'))?.hide();
                Utils.flash(`Đăng ký thành công! Chào mừng ${_user.username}!`, 'success');
                form.reset();
                setTimeout(() => location.reload(), 800);
            } catch (err) {
                if (errEl) { errEl.textContent = err.message; errEl.classList.remove('d-none'); }
            } finally {
                spinner?.classList.add('d-none');
                if (btn) btn.disabled = false;
            }
        });
    }

    function setupLogout() {
        const btn = document.getElementById('btnLogout');
        if (!btn) return;
        btn.addEventListener('click', async e => {
            e.preventDefault();
            try {
                await Api.post('/api/user/logout');
                _user = null;
                Utils.flash('Đã đăng xuất.', 'info');
                setTimeout(() => { location.href = '/'; }, 600);
            } catch (err) {
                Utils.flash('Đăng xuất thất bại: ' + err.message, 'danger');
            }
        });
    }

    function setupPasswordToggles() {
        document.querySelectorAll('.toggle-pwd-btn').forEach(btn => {
            btn.addEventListener('click', () => {
                const targetId = btn.dataset.target;
                const el       = document.getElementById(targetId);
                if (!el) return;
                const show         = el.type === 'password';
                el.type            = show ? 'text' : 'password';
                const icon         = btn.querySelector('i');
                if (icon) icon.className = show ? 'bi bi-eye-slash' : 'bi bi-eye';
            });
        });
    }

    function setupSearch() {
        function doSearch(input) {
            const q = (input?.value || '').trim().toLowerCase();
            document.querySelectorAll('.topic-row').forEach(row => {
                const title = row.querySelector('.topic-title')?.textContent.toLowerCase() || '';
                const meta  = row.querySelector('.topic-meta')?.textContent.toLowerCase()  || '';
                row.style.display = (!q || title.includes(q) || meta.includes(q)) ? '' : 'none';
            });
        }

        const btn   = document.getElementById('searchBtn');
        const input = document.getElementById('searchInput');
        if (btn && input) {
            btn.addEventListener('click', () => doSearch(input));
            input.addEventListener('keydown', e => { if (e.key === 'Enter') doSearch(input); });
        }

        const btnM   = document.getElementById('searchBtnMobile');
        const inputM = document.getElementById('searchInputMobile');
        if (btnM && inputM) {
            btnM.addEventListener('click', () => doSearch(inputM));
            inputM.addEventListener('keydown', e => { if (e.key === 'Enter') doSearch(inputM); });
        }
    }

    return {
        get user() { return _user; },
        init,
        setupLoginForm,
        setupRegisterForm,
        setupLogout,
        setupPasswordToggles,
        setupSearch
    };
})();

/* ── Forum (Homepage) ────────────────────────────────────────────
   Loads and renders the paginated topic list on /.
   ─────────────────────────────────────────────────────────────── */
const Forum = (() => {
    let _page     = 1;
    let _pageSize = 20;

    async function init() {
        /* Page-size selector */
        const select = document.getElementById('pageSizeSelect');
        if (select) {
            // Sync initial value in case user previously changed it
            _pageSize = parseInt(select.value, 10) || 20;
            select.addEventListener('change', () => {
                _pageSize = parseInt(select.value, 10) || 20;
                _load(1);
            });
        }

        /* Empty-state "post first" button */
        const emptyBtn = document.getElementById('emptyPostBtn');
        if (emptyBtn) {
            emptyBtn.addEventListener('click', () => {
                if (Auth.user) {
                    location.href = '/post/create';
                } else {
                    const modal = document.getElementById('loginModal');
                    modal && bootstrap.Modal.getOrCreateInstance(modal).show();
                }
            });
        }

        Auth.setupSearch();
        await _load(1);
    }

    async function _load(page) {
        _page = page;
        const list     = document.getElementById('topicList');
        const empty    = document.getElementById('emptyState');
        const pagWrap  = document.getElementById('paginationWrapper');
        if (!list) return;

        list.innerHTML = `
            <div class="d-flex justify-content-center align-items-center py-5">
                <div class="spinner-border text-primary" role="status">
                    <span class="visually-hidden">Đang tải...</span>
                </div>
            </div>`;
        empty?.classList.add('d-none');
        pagWrap?.classList.add('d-none');

        try {
            const res   = await Api.get(`/api/post?page=${page}&pageSize=${_pageSize}`);
            const paged = res.data;

            if (!paged.items || paged.items.length === 0) {
                list.innerHTML = '';
                empty?.classList.remove('d-none');
                _updateStats(0, 0, 0);
                return;
            }

            list.innerHTML = paged.items.map(_renderRow).join('');

            /* Wire click on each row */
            list.querySelectorAll('.topic-row[data-post-id]').forEach(row => {
                row.addEventListener('click', e => {
                    if (!e.target.closest('a'))
                        location.href = `/post/${row.dataset.slug}/${row.dataset.postId}`;
                });
            });

            _renderPagination(paged);

            /* Approximate stats from current page */
            const replies = paged.items.reduce((s, p) => s + (p.commentCount || 0), 0);
            const views   = paged.items.reduce((s, p) => s + (p.viewCount   || 0), 0);
            _updateStats(paged.totalCount, replies, views);

        } catch (err) {
            list.innerHTML = `
                <div class="text-center py-5 text-muted">
                    <i class="bi bi-exclamation-triangle fs-4 d-block mb-2"></i>
                    Không thể tải bài viết: ${Utils.escapeHtml(err.message)}
                </div>`;
        }
    }

    function _renderRow(post) {
        const avatar   = Utils.avatarHtml(post.username, 'topic-avatar');
        const title    = Utils.escapeHtml(post.title);
        const author   = Utils.escapeHtml(post.username);
        const timeStr  = Utils.timeAgo(post.createdAt);

        return `
        <div class="topic-row" data-post-id="${post.id}" data-slug="${Utils.slugify(post.title)}">
            <div class="topic-avatar-wrap">${avatar}</div>
            <div class="topic-body">
                <div class="topic-title">${title}</div>
                <div class="topic-meta">
                    <span class="fw-medium">${author}</span>
                    &middot; ${timeStr}
                </div>
            </div>
            <div class="topic-stat-col d-none d-md-block">
                <div class="topic-stat-val">${Utils.fmtNum(post.commentCount || 0)}</div>
                <div class="topic-stat-lbl">trả lời</div>
            </div>
            <div class="topic-stat-col d-none d-md-block">
                <div class="topic-stat-val">${Utils.fmtNum(post.viewCount || 0)}</div>
                <div class="topic-stat-lbl">lượt xem</div>
            </div>
            <div class="topic-activity d-none d-md-block">${timeStr}</div>
        </div>`;
    }

    function _renderPagination(paged) {
        const wrap = document.getElementById('paginationWrapper');
        const info = document.getElementById('paginationInfo');
        const ul   = document.getElementById('pagination');
        if (!wrap || !info || !ul) return;

        if (paged.totalPages <= 1) { wrap.classList.add('d-none'); return; }

        wrap.classList.remove('d-none');

        const start = (paged.page - 1) * paged.pageSize + 1;
        const end   = Math.min(paged.page * paged.pageSize, paged.totalCount);
        info.textContent = `Hiển thị ${start}–${end} / ${paged.totalCount} bài viết`;

        const cur   = paged.page;
        const total = paged.totalPages;
        const items = [];

        /* Prev */
        items.push(`<li class="page-item ${cur === 1 ? 'disabled' : ''}">
            <a class="page-link" href="#" data-page="${cur - 1}">
                <i class="bi bi-chevron-left"></i>
            </a></li>`);

        /* Numbers with ellipsis */
        const range = new Set([1, total]);
        for (let p = Math.max(2, cur - 2); p <= Math.min(total - 1, cur + 2); p++) range.add(p);
        const sorted = [...range].sort((a, b) => a - b);
        let prev = 0;
        sorted.forEach(p => {
            if (p - prev > 1)
                items.push(`<li class="page-item disabled"><span class="page-link">&hellip;</span></li>`);
            items.push(`<li class="page-item ${p === cur ? 'active' : ''}">
                <a class="page-link" href="#" data-page="${p}">${p}</a></li>`);
            prev = p;
        });

        /* Next */
        items.push(`<li class="page-item ${cur === total ? 'disabled' : ''}">
            <a class="page-link" href="#" data-page="${cur + 1}">
                <i class="bi bi-chevron-right"></i>
            </a></li>`);

        ul.innerHTML = items.join('');

        ul.querySelectorAll('.page-link[data-page]').forEach(link => {
            link.addEventListener('click', e => {
                e.preventDefault();
                const p = parseInt(link.dataset.page, 10);
                if (p >= 1 && p <= total && p !== cur) {
                    _load(p);
                    window.scrollTo({ top: 0, behavior: 'smooth' });
                }
            });
        });
    }

    function _updateStats(topics, replies, views) {
        const el = id => document.getElementById(id);
        el('statTopics')  && (el('statTopics').textContent  = Utils.fmtNum(topics));
        el('statReplies') && (el('statReplies').textContent = Utils.fmtNum(replies));
        el('statViews')   && (el('statViews').textContent   = Utils.fmtNum(views));
    }

    return { init };
})();

/* ── Quill toolbar config ────────────────────────────────────────
   Shared toolbar definition used by both CreatePost and PostDetail.
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

/* ── PostDetail ──────────────────────────────────────────────────
   Loads a post + its comments and wires CRUD interactions.
   ─────────────────────────────────────────────────────────────── */
const PostDetail = (() => {
    let _postId = 0;

    async function init(postId) {
        _postId = postId;
        await _load();
    }

    async function _load() {
        const container = document.getElementById('postContainer');
        if (!container) return;

        try {
            const res  = await Api.get(`/api/post/${_postId}`);
            const post = res.data;

            /* Breadcrumb title */
            const bc = document.getElementById('breadcrumbTitle');
            if (bc) bc.textContent =
                post.title.length > 60 ? post.title.substring(0, 60) + '…' : post.title;

            /* Update page title */
            document.title = `${post.title} — Forum`;

            /* Canonicalize slug — silently correct the URL if it doesn't match */
            const correctSlug = Utils.slugify(post.title);
            const canonical   = `/post/${correctSlug}/${_postId}`;
            if (window.location.pathname !== canonical) {
                history.replaceState(null, '', canonical);
            }

            _renderPost(post);
            _showCommentSection(post.comments || []);

        } catch (err) {
            container.innerHTML = `
                <div class="forum-card p-5 text-center">
                    <i class="bi bi-exclamation-triangle fs-1 text-danger d-block mb-3"></i>
                    <h5>Không tìm thấy bài viết</h5>
                    <p class="text-muted">${Utils.escapeHtml(err.message)}</p>
                    <a href="/" class="btn btn-primary">Quay về trang chủ</a>
                </div>`;
        }
    }

    function _renderPost(post) {
        const container = document.getElementById('postContainer');
        if (!container) return;

        const avatar     = Utils.avatarHtml(post.username);
        const isOwner    = Auth.user && Auth.user.id === post.userId;
        const isAdmin    = Auth.user && Auth.user.role === 'Admin';
        const canEdit    = isOwner;                   // only owner
        const canDelete  = isOwner || isAdmin;        // owner or admin
        const canClose   = isAdmin;                   // admin only
        const edited     = post.updatedAt
            ? `<span class="fst-italic text-muted small ms-1">(đã chỉnh sửa)</span>` : '';
        const closedBadge = post.isClosed
            ? `<span class="badge bg-secondary ms-2"><i class="bi bi-lock me-1"></i>Đã đóng</span>` : '';

        container.innerHTML = `
        <div class="post-detail-card">
            <div class="post-header">
                <h1 class="post-title">${Utils.escapeHtml(post.title)}${closedBadge}</h1>
                <div class="post-author-row">
                    ${avatar}
                    <div>
                        <div class="post-author-name">${Utils.escapeHtml(post.username)}</div>
                        <div class="post-author-meta">${Utils.timeAgo(post.createdAt)} ${edited}</div>
                    </div>
                </div>
            </div>
            <div class="post-body" id="postBodyContent"></div>
            <div class="post-footer">
                <div class="post-stats">
                    <span><i class="bi bi-eye me-1"></i>${Utils.fmtNum(post.viewCount)} lượt xem</span>
                    <span><i class="bi bi-chat me-1"></i>${post.commentCount || 0} bình luận</span>
                </div>
                ${(canEdit || canDelete || canClose) ? `
                <div class="post-actions">
                    ${canEdit ? `
                    <button class="btn btn-outline-secondary btn-sm" id="btnEditPost">
                        <i class="bi bi-pencil me-1"></i>Sửa
                    </button>` : ''}
                    ${canDelete ? `
                    <button class="btn btn-outline-danger btn-sm" id="btnDeletePost">
                        <i class="bi bi-trash me-1"></i>Xóa
                    </button>` : ''}
                    ${canClose ? `
                    <button class="btn btn-outline-warning btn-sm" id="btnClosePost">
                        <i class="bi bi-${post.isClosed ? 'unlock' : 'lock'} me-1"></i>${post.isClosed ? 'Mở lại' : 'Đóng'}
                    </button>` : ''}
                </div>` : ''}
            </div>
        </div>`;

        // Use innerHTML for server-sanitized rich HTML (safe — Ganss.Xss sanitizes on server before DB)
        const bodyEl = document.getElementById('postBodyContent');
        if (bodyEl) bodyEl.innerHTML = post.content;

        if (canEdit)   document.getElementById('btnEditPost')?.addEventListener('click', () => _showEditPost(post));
        if (canDelete) document.getElementById('btnDeletePost')?.addEventListener('click', () => _deletePost());
        if (canClose)  document.getElementById('btnClosePost')?.addEventListener('click', () => _toggleClose(post));
    }

    function _showCommentSection(comments) {
        /* Comment count */
        const section  = document.getElementById('commentsSection');
        const countEl  = document.getElementById('commentCount');
        section?.classList.remove('d-none');
        if (countEl) countEl.textContent = comments.length;

        /* Render comments */
        const container = document.getElementById('commentsContainer');
        if (container) {
            if (comments.length === 0) {
                container.innerHTML = `
                    <div class="text-center py-4 text-muted small">
                        <i class="bi bi-chat-square d-block fs-4 mb-2"></i>
                        Chưa có bình luận nào. Hãy là người đầu tiên!
                    </div>`;
            } else {
                container.innerHTML = comments.map(_renderComment).join('');
                _wireCommentActions(container);
            }
        }

        /* Show form or guest prompt */
        const formSection   = document.getElementById('commentFormSection');
        const guestPrompt   = document.getElementById('commentGuestPrompt');
        const commentAvatar = document.getElementById('commentAvatar');

        if (Auth.user) {
            formSection?.classList.remove('d-none');
            guestPrompt?.classList.add('d-none');
            if (commentAvatar) {
                commentAvatar.textContent        = Auth.user.username[0].toUpperCase();
                commentAvatar.style.background   = Utils.strToColor(Auth.user.username);
            }
            _setupCommentForm();
        } else {
            formSection?.classList.add('d-none');
            guestPrompt?.classList.remove('d-none');
        }
    }

    function _renderComment(comment) {
        const avatar   = Utils.avatarHtml(comment.username, 'comment-avatar');
        const canEdit  = Auth.user && (Auth.user.id === comment.userId || Auth.user.role === 'Admin');
        const edited   = comment.updatedAt
            ? `<span class="comment-edited">(đã sửa)</span>` : '';

        return `
        <div class="comment-card" data-comment-id="${comment.id}">
            ${avatar}
            <div class="comment-body">
                <div class="comment-header">
                    <span class="comment-author">${Utils.escapeHtml(comment.username)}</span>
                    <span class="comment-time">${Utils.timeAgo(comment.createdAt)}</span>
                    ${edited}
                </div>
                <div class="comment-text">${Utils.escapeHtml(comment.content)}</div>
                ${canEdit ? `
                <div class="comment-actions">
                    <button class="comment-action-btn comment-btn-edit">
                        <i class="bi bi-pencil me-1"></i>Sửa
                    </button>
                    <button class="comment-action-btn danger comment-btn-delete">
                        <i class="bi bi-trash me-1"></i>Xóa
                    </button>
                </div>` : ''}
                <div class="comment-edit-form d-none"></div>
            </div>
        </div>`;
    }

    function _wireCommentActions(container) {
        container.querySelectorAll('.comment-card[data-comment-id]').forEach(card => {
            const cid = parseInt(card.dataset.commentId, 10);

            card.querySelector('.comment-btn-edit')?.addEventListener('click', () => {
                /* Read current DOM text to handle already-edited comments */
                const currentText = card.querySelector('.comment-text')?.textContent || '';
                _showEditComment(cid, currentText, card);
            });

            card.querySelector('.comment-btn-delete')?.addEventListener('click', () => {
                _deleteComment(cid, card);
            });
        });
    }

    function _setupCommentForm() {
        const input   = document.getElementById('commentInput');
        const btn     = document.getElementById('btnSubmitComment');
        const charEl  = document.getElementById('commentCharCount');
        const spinner = document.getElementById('commentSpinner');
        if (!input || !btn) return;

        input.addEventListener('input', () => {
            if (charEl) charEl.textContent = `${input.value.length} / 5000`;
        });

        btn.addEventListener('click', async () => {
            const content = input.value.trim();
            if (!content) { input.focus(); return; }
            if (content.length > 5000) {
                Utils.flash('Bình luận không được quá 5000 ký tự.', 'warning');
                return;
            }
            try {
                spinner?.classList.remove('d-none');
                btn.disabled = true;

                await Api.post('/api/comment', { postId: _postId, content });
                input.value = '';
                if (charEl) charEl.textContent = '0 / 5000';

                /* Reload to show new comment */
                await _load();
                document.getElementById('commentsSection')
                    ?.scrollIntoView({ behavior: 'smooth', block: 'start' });

            } catch (err) {
                Utils.flash('Gửi bình luận thất bại: ' + err.message, 'danger');
            } finally {
                spinner?.classList.add('d-none');
                btn.disabled = false;
            }
        });
    }

    async function _deletePost() {
        if (!confirm('Bạn có chắc muốn xóa bài viết này? Hành động này không thể hoàn tác.')) return;
        try {
            await Api.del(`/api/post/${_postId}`);
            Utils.flash('Đã xóa bài viết.', 'success');
            setTimeout(() => { location.href = '/'; }, 800);
        } catch (err) {
            Utils.flash('Xóa thất bại: ' + err.message, 'danger');
        }
    }

    async function _toggleClose(post) {
        const action = post.isClosed ? 'mở lại' : 'đóng';
        if (!confirm(`Bạn có chắc muốn ${action} bài viết này?`)) return;
        try {
            await Api.put(`/api/post/${_postId}/close`);
            await _load();
        } catch (err) {
            Utils.flash(`Thất bại: ${err.message}`, 'danger');
        }
    }

    let _editQuill = null;   // Quill instance for the edit-post modal

    function _showEditPost(post) {
        const modal = document.getElementById('editPostModal');
        if (!modal) return;

        document.getElementById('editPostTitle').value = post.title;
        document.getElementById('editPostError')?.classList.add('d-none');

        /* Clone to remove stale listeners on the save button */
        const oldBtn = document.getElementById('btnSaveEditPost');
        const newBtn = oldBtn.cloneNode(true);
        oldBtn.parentNode.replaceChild(newBtn, oldBtn);
        newBtn.addEventListener('click', _saveEditPost);

        /* Initialise (or re-use) the Quill editor inside the modal */
        const editorEl = document.getElementById('editPostContentEditor');
        if (!editorEl) { bootstrap.Modal.getOrCreateInstance(modal).show(); return; }

        if (!_editQuill) {
            _editQuill = new Quill('#editPostContentEditor', {
                theme: 'snow',
                modules: { toolbar: _quillToolbar() }
            });
            _editQuill.on('text-change', () => {
                const input = document.getElementById('editPostContent');
                if (input) input.value = _editQuill.root.innerHTML;
            });
        }

        /* Load existing content into the editor */
        _editQuill.clipboard.dangerouslyPasteHTML(post.content || '');
        const input = document.getElementById('editPostContent');
        if (input) input.value = _editQuill.root.innerHTML;

        bootstrap.Modal.getOrCreateInstance(modal).show();
    }

    async function _saveEditPost() {
        const title   = document.getElementById('editPostTitle')?.value.trim();
        const content = document.getElementById('editPostContent')?.value.trim();
        const errEl   = document.getElementById('editPostError');
        const spinner = document.getElementById('editPostSpinner');
        const btn     = document.getElementById('btnSaveEditPost');

        /* Validate visible text length (strip tags) */
        const visibleText = _editQuill ? _editQuill.getText().trim() : '';
        if (!title || visibleText.length < 10) return;

        try {
            errEl?.classList.add('d-none');
            spinner?.classList.remove('d-none');
            if (btn) btn.disabled = true;

            await Api.put(`/api/post/${_postId}`, { title, content });
            bootstrap.Modal.getOrCreateInstance(document.getElementById('editPostModal'))?.hide();
            Utils.flash('Đã cập nhật bài viết.', 'success');
            await _load();

        } catch (err) {
            if (errEl) { errEl.textContent = err.message; errEl.classList.remove('d-none'); }
        } finally {
            spinner?.classList.add('d-none');
            if (btn) btn.disabled = false;
        }
    }

    function _showEditComment(commentId, currentContent, cardEl) {
        const editForm = cardEl.querySelector('.comment-edit-form');
        if (!editForm) return;

        /* Toggle off if already open */
        if (!editForm.classList.contains('d-none')) {
            editForm.classList.add('d-none');
            editForm.innerHTML = '';
            return;
        }

        const escaped = Utils.escapeHtml(currentContent);
        editForm.innerHTML = `
            <textarea class="form-control form-control-sm mb-2" rows="3">${escaped}</textarea>
            <div class="d-flex gap-2">
                <button class="btn btn-primary btn-sm comment-save-btn">
                    <span class="spinner-border spinner-border-sm me-1 d-none"></span>Lưu
                </button>
                <button class="btn btn-secondary btn-sm comment-cancel-btn">Hủy</button>
            </div>`;
        editForm.classList.remove('d-none');

        editForm.querySelector('.comment-cancel-btn')?.addEventListener('click', () => {
            editForm.classList.add('d-none');
            editForm.innerHTML = '';
        });

        editForm.querySelector('.comment-save-btn')?.addEventListener('click', async () => {
            const ta      = editForm.querySelector('textarea');
            const content = ta?.value.trim();
            if (!content) return;

            const spinner = editForm.querySelector('.spinner-border');
            const saveBtn = editForm.querySelector('.comment-save-btn');

            try {
                spinner?.classList.remove('d-none');
                if (saveBtn) saveBtn.disabled = true;

                await Api.put(`/api/comment/${commentId}`, { content });

                /* Update DOM in-place — no full reload needed */
                const textEl = cardEl.querySelector('.comment-text');
                if (textEl) textEl.textContent = content;

                const header = cardEl.querySelector('.comment-header');
                if (header && !header.querySelector('.comment-edited')) {
                    header.insertAdjacentHTML('beforeend', `<span class="comment-edited">(đã sửa)</span>`);
                }

                editForm.classList.add('d-none');
                editForm.innerHTML = '';
                Utils.flash('Đã cập nhật bình luận.', 'success');

            } catch (err) {
                Utils.flash('Cập nhật thất bại: ' + err.message, 'danger');
                spinner?.classList.add('d-none');
                if (saveBtn) saveBtn.disabled = false;
            }
        });
    }

    async function _deleteComment(commentId, cardEl) {
        if (!confirm('Xóa bình luận này?')) return;
        try {
            await Api.del(`/api/comment/${commentId}`);
            cardEl.remove();

            const countEl = document.getElementById('commentCount');
            if (countEl) {
                const n = Math.max(0, parseInt(countEl.textContent, 10) - 1);
                countEl.textContent = n;
            }
            Utils.flash('Đã xóa bình luận.', 'success');
        } catch (err) {
            Utils.flash('Xóa thất bại: ' + err.message, 'danger');
        }
    }

    return { init };
})();

/* ── UserProfile ─────────────────────────────────────────────────
   Loads public profile and paginated post list for /profile/{id}.
   ─────────────────────────────────────────────────────────────── */
const UserProfile = (() => {
    let _userId   = 0;
    let _pageSize = 20;

    async function init(userId) {
        _userId = userId;
        await Promise.all([_loadProfile(), _loadPosts(1)]);
    }

    async function _loadProfile() {
        const header = document.getElementById('profileHeader');
        if (!header) return;

        try {
            const res  = await Api.get(`/api/user/profile/${_userId}`);
            const user = res.data;

            const bc = document.getElementById('breadcrumbUsername');
            if (bc) bc.textContent = user.username;
            document.title = `${user.username} — Forum`;

            const avatar  = Utils.avatarHtml(user.username, 'profile-avatar');
            const roleCls = (user.role || 'user').toLowerCase();

            header.innerHTML = `
            <div class="forum-card p-4 mb-2">
                <div class="d-flex align-items-center gap-4 flex-wrap">
                    ${avatar}
                    <div class="flex-grow-1">
                        <div class="d-flex align-items-center gap-2 flex-wrap">
                            <h4 class="mb-0 fw-bold">${Utils.escapeHtml(user.username)}</h4>
                            <span class="badge badge-role-${roleCls}">${Utils.escapeHtml(user.role)}</span>
                        </div>
                        <div class="text-muted small mt-1">
                            <i class="bi bi-calendar3 me-1"></i>Tham gia ${Utils.timeAgo(user.createdAt)}
                        </div>
                    </div>
                    <div class="d-flex gap-4 flex-wrap text-center">
                        <div>
                            <div class="fs-5 fw-bold text-primary">${Utils.fmtNum(user.postCount)}</div>
                            <div class="text-muted small">bài viết</div>
                        </div>
                        <div>
                            <div class="fs-5 fw-bold text-primary">${Utils.fmtNum(user.commentCount)}</div>
                            <div class="text-muted small">bình luận</div>
                        </div>
                    </div>
                </div>
            </div>`;
        } catch {
            const header = document.getElementById('profileHeader');
            if (header) header.innerHTML = `
            <div class="forum-card p-5 text-center">
                <i class="bi bi-person-x fs-1 text-danger d-block mb-3"></i>
                <h5>Không tìm thấy người dùng</h5>
                <a href="/" class="btn btn-primary mt-2">Quay về trang chủ</a>
            </div>`;
        }
    }

    async function _loadPosts(page) {
        const section = document.getElementById('profilePostSection');
        const listEl  = document.getElementById('profilePostList');
        const totalEl = document.getElementById('profilePostTotal');
        if (!listEl) return;

        listEl.innerHTML = `
            <div class="d-flex justify-content-center py-4">
                <div class="spinner-border text-primary spinner-border-sm" role="status"></div>
            </div>`;

        try {
            const res   = await Api.get(`/api/post/user/${_userId}?page=${page}&pageSize=${_pageSize}`);
            const paged = res.data;

            section?.classList.remove('d-none');
            if (totalEl) totalEl.textContent = paged.totalCount;

            if (!paged.items || paged.items.length === 0) {
                listEl.innerHTML = `
                    <div class="text-center py-4 text-muted small">
                        <i class="bi bi-inbox d-block fs-4 mb-2"></i>Chưa có bài viết nào.
                    </div>`;
                return;
            }

            listEl.innerHTML = paged.items.map(_renderRow).join('');

            listEl.querySelectorAll('.topic-row[data-post-id]').forEach(row => {
                row.addEventListener('click', e => {
                    if (!e.target.closest('a'))
                        location.href = `/post/${row.dataset.slug}/${row.dataset.postId}`;
                });
            });

            _renderPagination(paged);

        } catch (err) {
            listEl.innerHTML = `
                <div class="text-center py-4 text-muted small">
                    <i class="bi bi-exclamation-triangle me-1"></i>${Utils.escapeHtml(err.message)}
                </div>`;
        }
    }

    function _renderRow(post) {
        const title   = Utils.escapeHtml(post.title);
        const timeStr = Utils.timeAgo(post.createdAt);
        return `
        <div class="topic-row" data-post-id="${post.id}" data-slug="${Utils.slugify(post.title)}">
            <div class="topic-body" style="padding-left:0">
                <div class="topic-title">${title}</div>
                <div class="topic-meta">${timeStr}</div>
            </div>
            <div class="topic-stat-col d-none d-md-block">
                <div class="topic-stat-val">${Utils.fmtNum(post.commentCount || 0)}</div>
                <div class="topic-stat-lbl">trả lời</div>
            </div>
            <div class="topic-stat-col d-none d-md-block">
                <div class="topic-stat-val">${Utils.fmtNum(post.viewCount || 0)}</div>
                <div class="topic-stat-lbl">lượt xem</div>
            </div>
            <div class="topic-activity d-none d-md-block">${timeStr}</div>
        </div>`;
    }

    function _renderPagination(paged) {
        const wrap = document.getElementById('profilePaginationWrapper');
        const info = document.getElementById('profilePaginationInfo');
        const ul   = document.getElementById('profilePagination');
        if (!wrap || !info || !ul) return;

        if (paged.totalPages <= 1) { wrap.classList.add('d-none'); return; }
        wrap.classList.remove('d-none');

        const cur   = paged.page;
        const total = paged.totalPages;
        const start = (cur - 1) * paged.pageSize + 1;
        const end   = Math.min(cur * paged.pageSize, paged.totalCount);
        info.textContent = `Hiển thị ${start}–${end} / ${paged.totalCount} bài viết`;

        const items = [];
        items.push(`<li class="page-item ${cur === 1 ? 'disabled' : ''}">
            <a class="page-link" href="#" data-page="${cur - 1}"><i class="bi bi-chevron-left"></i></a></li>`);

        const range = new Set([1, total]);
        for (let p = Math.max(2, cur - 2); p <= Math.min(total - 1, cur + 2); p++) range.add(p);
        [...range].sort((a, b) => a - b).reduce((prev, p) => {
            if (p - prev > 1)
                items.push(`<li class="page-item disabled"><span class="page-link">&hellip;</span></li>`);
            items.push(`<li class="page-item ${p === cur ? 'active' : ''}">
                <a class="page-link" href="#" data-page="${p}">${p}</a></li>`);
            return p;
        }, 0);

        items.push(`<li class="page-item ${cur === total ? 'disabled' : ''}">
            <a class="page-link" href="#" data-page="${cur + 1}"><i class="bi bi-chevron-right"></i></a></li>`);

        ul.innerHTML = items.join('');
        ul.querySelectorAll('.page-link[data-page]').forEach(link => {
            link.addEventListener('click', e => {
                e.preventDefault();
                const p = parseInt(link.dataset.page, 10);
                if (p >= 1 && p <= total && p !== cur) {
                    _loadPosts(p);
                    window.scrollTo({ top: 0, behavior: 'smooth' });
                }
            });
        });
    }

    return { init };
})();

/* ── CreatePost ──────────────────────────────────────────────────
   Handles the /post/create page.
   ─────────────────────────────────────────────────────────────── */
const CreatePost = (() => {
    function init() {
        const loginPrompt = document.getElementById('createPostLoginPrompt');
        const formWrapper = document.getElementById('createPostFormWrapper');
        if (!loginPrompt || !formWrapper) return;

        if (!Auth.user) {
            loginPrompt.classList.remove('d-none');
            formWrapper.classList.add('d-none');
            return;
        }

        loginPrompt.classList.add('d-none');
        formWrapper.classList.remove('d-none');

        const titleInput   = document.getElementById('postTitle');
        const contentInput = document.getElementById('postContent');   // hidden input
        const titleCount   = document.getElementById('titleCharCount');
        const contentCount = document.getElementById('contentCharCount');
        const form         = document.getElementById('createPostForm');
        const errEl        = document.getElementById('createPostError');
        const spinner      = document.getElementById('createSpinner');
        const btn          = document.getElementById('btnCreatePost');

        /* ── Quill rich text editor ── */
        const quill = new Quill('#postContentEditor', {
            theme: 'snow',
            placeholder: 'Nội dung bài viết (tối thiểu 10 ký tự)...',
            modules: { toolbar: _quillToolbar() }
        });

        quill.on('text-change', () => {
            const html = quill.root.innerHTML;
            if (contentInput) contentInput.value = html;
            if (contentCount) contentCount.textContent = `${quill.getText().trim().length} ký tự`;
        });

        titleInput?.addEventListener('input', () => {
            if (titleCount) titleCount.textContent = `${titleInput.value.length} / 300`;
        });

        form?.addEventListener('submit', async e => {
            e.preventDefault();
            const title       = titleInput?.value.trim();
            const content     = contentInput?.value.trim();
            const visibleText = quill.getText().trim();

            if (!title || visibleText.length < 10) {
                if (errEl) {
                    errEl.textContent = !title
                        ? 'Vui lòng nhập tiêu đề.'
                        : 'Nội dung phải có ít nhất 10 ký tự.';
                    errEl.classList.remove('d-none');
                }
                return;
            }

            try {
                errEl?.classList.add('d-none');
                spinner?.classList.remove('d-none');
                if (btn) btn.disabled = true;

                const res = await Api.post('/api/post', { title, content });
                Utils.flash('Đã đăng bài viết!', 'success');
                setTimeout(() => { location.href = `/post/${Utils.slugify(res.data.title)}/${res.data.id}`; }, 500);

            } catch (err) {
                if (errEl) { errEl.textContent = err.message; errEl.classList.remove('d-none'); }
            } finally {
                spinner?.classList.add('d-none');
                if (btn) btn.disabled = false;
            }
        });
    }

    return { init };
})();

/* ── Bootstrap ───────────────────────────────────────────────────
   Wire everything on DOMContentLoaded, then detect current page
   and initialise the appropriate module.
   ─────────────────────────────────────────────────────────────── */
document.addEventListener('DOMContentLoaded', async () => {
    /* Global wiring (runs on every page) */
    Auth.setupPasswordToggles();
    Auth.setupLoginForm();
    Auth.setupRegisterForm();
    Auth.setupLogout();

    /* "Go back" button on error page */
    document.getElementById('btnGoBack')?.addEventListener('click', e => {
        e.preventDefault();
        history.back();
    });

    /* Resolve auth state before rendering */
    await Auth.init();

    /* Page-specific initialisation */
    const path = window.location.pathname.replace(/\/+$/, '') || '/';

    if (path === '' || path === '/' || /^\/home(\/index)?$/i.test(path)) {
        await Forum.init();

    } else if (/^\/post\/create$/i.test(path)) {
        CreatePost.init();

    } else {
        const m = path.match(/^\/post\/[^/]+\/(\d+)$/i);
        if (m) { await PostDetail.init(parseInt(m[1], 10)); return; }

        const mp = path.match(/^\/profile\/(\d+)$/i);
        if (mp) await UserProfile.init(parseInt(mp[1], 10));
    }
});
