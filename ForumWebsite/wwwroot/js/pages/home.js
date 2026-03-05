/* ═══════════════════════════════════════════════════════════════
   wwwroot/js/pages/home.js
   Homepage — paginated topic list.
   Depends on: ApiService, PostService, Utils, Auth (via Auth.ready)
   ════════════════════════════════════════════════════════════════ */

'use strict';

/* ── Forum (Homepage) ────────────────────────────────────────────
   Loads and renders the paginated topic list on /.
   ─────────────────────────────────────────────────────────────── */
const Forum = (() => {
    let _page     = 1;
    let _pageSize = 20;

    async function init() {
        const select = document.getElementById('pageSizeSelect');
        if (select) {
            _pageSize = parseInt(select.value, 10) || 20;
            select.addEventListener('change', () => {
                _pageSize = parseInt(select.value, 10) || 20;
                _load(1);
            });
        }

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
        const list    = document.getElementById('topicList');
        const empty   = document.getElementById('emptyState');
        const pagWrap = document.getElementById('paginationWrapper');
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
            const res   = await PostService.getAll(page, _pageSize);
            const paged = res.data;

            if (!paged.items || paged.items.length === 0) {
                list.innerHTML = '';
                empty?.classList.remove('d-none');
                _updateStats(0, 0, 0);
                return;
            }

            list.innerHTML = paged.items.map(_renderRow).join('');

            list.querySelectorAll('.topic-row[data-post-id]').forEach(row => {
                row.addEventListener('click', e => {
                    if (!e.target.closest('a'))
                        location.href = `/post/${row.dataset.slug}/${row.dataset.postId}`;
                });
            });

            _renderPagination(paged);

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
        const avatar  = Utils.avatarHtml(post.username, 'topic-avatar');
        const title   = Utils.escapeHtml(post.title);
        const author  = Utils.escapeHtml(post.username);
        const timeStr = Utils.timeAgo(post.createdAt);

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
        // Delegates to shared Utils.renderPagination — no duplication with user-profile.js
        Utils.renderPagination(
            paged,
            { wrap: 'paginationWrapper', info: 'paginationInfo', ul: 'pagination' },
            p => _load(p),
            'bài viết'
        );
    }

    function _updateStats(topics, replies, views) {
        const el = id => document.getElementById(id);
        el('statTopics')  && (el('statTopics').textContent  = Utils.fmtNum(topics));
        el('statReplies') && (el('statReplies').textContent = Utils.fmtNum(replies));
        el('statViews')   && (el('statViews').textContent   = Utils.fmtNum(views));
    }

    return { init };
})();

document.addEventListener('DOMContentLoaded', async () => {
    await Auth.ready;
    await Forum.init();
});
