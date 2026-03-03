/* ═══════════════════════════════════════════════════════════════
   wwwroot/js/pages/user-profile.js
   User profile page — public profile card + paginated post list.
   Depends on: ApiService, PostService, UserService, Utils, Auth
   ════════════════════════════════════════════════════════════════ */

'use strict';

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
            const res  = await UserService.getProfile(_userId);
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
            const res   = await PostService.getByUser(_userId, page, _pageSize);
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

document.addEventListener('DOMContentLoaded', async () => {
    await Auth.ready;
    const m = window.location.pathname.match(/^\/profile\/(\d+)$/i);
    if (m) await UserProfile.init(parseInt(m[1], 10));
});
