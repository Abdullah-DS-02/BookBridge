// ============================================
// BookBridge — Real-Time Notifications & Chat
// ============================================

(function () {
  'use strict';

  // ── Notification Hub ──────────────────────
  const notifConnection = new signalR.HubConnectionBuilder()
    .withUrl('/notificationHub')
    .withAutomaticReconnect()
    .build();

  notifConnection.start().catch(console.error);

  notifConnection.on('ReceiveNotification', (data) => {
    showToastNotification(data.title, data.body, data.type || 'info');
    updateNotifCount(1);
  });

  function showToastNotification(title, body, type) {
    const icons = {
      success: 'bi-check-circle-fill text-success',
      warning: 'bi-exclamation-triangle-fill text-warning',
      danger: 'bi-x-circle-fill text-danger',
      info: 'bi-info-circle-fill text-primary',
      borrow: 'bi-clock-fill text-warning'
    };

    const toast = document.createElement('div');
    toast.className = 'toast bb-toast show';
    toast.style.cssText = 'min-width:300px;';
    toast.innerHTML = `
      <div class="toast-header border-0">
        <i class="bi ${icons[type] || icons.info} me-2"></i>
        <strong class="me-auto">${escapeHtml(title)}</strong>
        <small class="text-muted">just now</small>
        <button type="button" class="btn-close" data-bs-dismiss="toast"></button>
      </div>
      <div class="toast-body">${escapeHtml(body)}</div>
    `;

    const container = document.querySelector('.toast-container');
    if (container) {
      container.appendChild(toast);
      setTimeout(() => {
        toast.style.transition = 'opacity 0.4s';
        toast.style.opacity = '0';
        setTimeout(() => toast.remove(), 400);
      }, 5000);
    }
  }

  function updateNotifCount(delta) {
    const badge = document.getElementById('notifCount');
    if (!badge) return;
    const current = parseInt(badge.textContent || '0');
    const next = current + delta;
    badge.textContent = next;
    badge.style.display = next > 0 ? 'flex' : 'none';
  }

  // ── Fetch initial unread count ─────────────
  async function fetchUnreadCount() {
    try {
      const res = await fetch('/Dashboard/GetUnreadCount');
      const data = await res.json();
      const badge = document.getElementById('notifCount');
      if (badge && data.count > 0) {
        badge.textContent = data.count;
        badge.style.display = 'flex';
      }
    } catch {}
  }

  fetchUnreadCount();

  // ── Chat Hub ───────────────────────────────
  if (document.querySelector('.chat-layout')) {
    initChatHub();
  }

  function initChatHub() {
    const chatConnection = new signalR.HubConnectionBuilder()
      .withUrl('/chatHub')
      .withAutomaticReconnect()
      .build();

    chatConnection.start().then(() => {
      const convId = document.querySelector('[data-conversation-id]')?.dataset.conversationId;
      if (convId) {
        chatConnection.invoke('JoinConversation', parseInt(convId));
      }
    }).catch(console.error);

    chatConnection.on('ReceiveMessage', (msg) => {
      appendMessage(msg);
      scrollChatToBottom();
    });

    chatConnection.on('UserTyping', (userId, isTyping) => {
      const indicator = document.getElementById('typingIndicator');
      if (indicator) {
        indicator.style.display = isTyping ? 'flex' : 'none';
      }
    });

    chatConnection.on('UserOnline', (userId) => {
      document.querySelectorAll(`[data-user-id="${userId}"] .online-dot`).forEach(dot => {
        dot.style.display = 'block';
      });
    });

    chatConnection.on('UserOffline', (userId) => {
      document.querySelectorAll(`[data-user-id="${userId}"] .online-dot`).forEach(dot => {
        dot.style.display = 'none';
      });
    });

    // Send message
    const sendBtn = document.getElementById('sendMessageBtn');
    const textarea = document.getElementById('chatTextarea');
    const fileInput = document.getElementById('fileUpload');
    const filePreviewBar = document.getElementById('filePreviewBar');
    const filePreviewName = document.getElementById('filePreviewName');
    const removeFileBtn = document.getElementById('removeFileBtn');
    let pendingFile = null;
    let typingTimer;

    // File selection → show preview bar
    if (fileInput) {
      fileInput.addEventListener('change', () => {
        if (!fileInput.files || fileInput.files.length === 0) {
          pendingFile = null;
          if (filePreviewBar) filePreviewBar.style.display = 'none';
          return;
        }
        pendingFile = fileInput.files[0];
        if (filePreviewName) filePreviewName.textContent = pendingFile.name;
        if (filePreviewBar) filePreviewBar.style.display = 'flex';
      });
    }

    // Remove file button
    if (removeFileBtn) {
      removeFileBtn.addEventListener('click', () => {
        pendingFile = null;
        if (fileInput) fileInput.value = '';
        if (filePreviewBar) filePreviewBar.style.display = 'none';
      });
    }

    if (sendBtn && textarea) {
      sendBtn.addEventListener('click', sendMessage);
      textarea.addEventListener('keydown', e => {
        if (e.key === 'Enter' && !e.shiftKey) {
          e.preventDefault();
          sendMessage();
        }
      });

      textarea.addEventListener('input', () => {
        const convId = document.querySelector('[data-conversation-id]')?.dataset.conversationId;
        if (convId) {
          chatConnection.invoke('Typing', parseInt(convId), true);
          clearTimeout(typingTimer);
          typingTimer = setTimeout(() => {
            chatConnection.invoke('Typing', parseInt(convId), false);
          }, 1500);
        }
      });
    }

    async function sendMessage() {
      if (!textarea) return;
      const content = textarea.value.trim();
      // Must have text or a file to send
      if (!content && !pendingFile) return;

      const convId = document.querySelector('[data-conversation-id]')?.dataset.conversationId;
      if (!convId) return;

      // Get CSRF token
      const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

      const originalBtnHtml = sendBtn ? sendBtn.innerHTML : '';
      if (sendBtn) {
        sendBtn.innerHTML = '<span class="spinner-border spinner-border-sm" role="status"></span>';
        sendBtn.disabled = true;
      }

      try {
        const formData = new FormData();
        formData.append('conversationId', convId);
        if (content) formData.append('content', content);
        if (pendingFile) formData.append('file', pendingFile);

        const headers = {};
        if (token) headers['RequestVerificationToken'] = token;

        const response = await fetch('/Chat/SendMessage', {
          method: 'POST',
          body: formData,
          headers: headers
        });

        if (response.ok) {
          textarea.value = '';
          textarea.style.height = 'auto';
          pendingFile = null;
          if (fileInput) fileInput.value = '';
          if (filePreviewBar) filePreviewBar.style.display = 'none';
          // Message will appear via SignalR ReceiveMessage broadcast
        } else {
          console.error('Send failed:', response.status, await response.text());
        }
      } catch (e) {
        console.error('Failed to send message', e);
      } finally {
        if (sendBtn) {
          sendBtn.innerHTML = originalBtnHtml;
          sendBtn.disabled = false;
        }
      }
    }

    function appendMessage(msg) {
      const currentUserId = document.querySelector('[data-current-user]')?.dataset.currentUser;
      const isOwn = msg.senderId === currentUserId;

      const messages = document.getElementById('chatMessages');
      if (!messages) return;

      const div = document.createElement('div');
      div.className = `chat-message ${isOwn ? 'own' : ''}`;

      let attachmentHtml = '';
      if (msg.imagePath) {
        const ext = msg.imagePath.split('.').pop().toLowerCase();
        const isImage = ['jpg', 'jpeg', 'png', 'gif', 'webp'].includes(ext);
        if (isImage) {
          attachmentHtml = `
            <div class="msg-bubble" style="padding:6px;">
              <img src="${msg.imagePath}" style="max-width:200px;border-radius:8px;cursor:zoom-in;" onclick="openLightbox(this.src)" />
            </div>`;
        } else {
          const fileName = msg.imagePath.split('/').pop();
          attachmentHtml = `
            <div class="msg-bubble">
              <a href="${msg.imagePath}" target="_blank" download class="d-flex align-items-center gap-2 text-decoration-none ${isOwn ? 'text-white' : 'text-primary'}">
                <i class="bi bi-file-earmark-arrow-down-fill" style="font-size:1.5rem;"></i>
                <span style="font-size:0.85rem;word-break:break-all;">${escapeHtml(fileName)}</span>
              </a>
            </div>`;
        }
      }

      let contentHtml = '';
      if (msg.content) {
        contentHtml = `<div class="msg-bubble">${escapeHtml(msg.content)}</div>`;
      }

      const bubbleContainer = `
        <div>
          ${attachmentHtml}
          ${contentHtml}
          <div class="msg-time">
            ${msg.sentAt}
            ${isOwn ? ' ✓' : ''}
          </div>
        </div>
      `;

      if (!isOwn) {
        const otherPic = document.querySelector('.chat-header img')?.src || '/images/default-avatar.png';
        div.innerHTML = `
          <img src="${otherPic}" class="msg-avatar" alt="User" />
          ${bubbleContainer}
        `;
      } else {
        div.innerHTML = bubbleContainer;
      }

      messages.appendChild(div);
      scrollChatToBottom();
    }

    function scrollChatToBottom() {
      const messages = document.getElementById('chatMessages');
      if (messages) {
        messages.scrollTop = messages.scrollHeight;
      }
    }

    // Scroll to bottom button
    const scrollBtn = document.getElementById('scrollToBottomBtn');
    const messagesEl = document.getElementById('chatMessages');
    if (messagesEl && scrollBtn) {
      messagesEl.addEventListener('scroll', () => {
        const threshold = 150;
        const scrolledFromBottom = messagesEl.scrollHeight - messagesEl.clientHeight - messagesEl.scrollTop;
        scrollBtn.style.display = scrolledFromBottom > threshold ? 'flex' : 'none';
      });

      scrollBtn.addEventListener('click', () => {
        messagesEl.scrollTo({ top: messagesEl.scrollHeight, behavior: 'smooth' });
      });
    }

    scrollChatToBottom();
  }

  function escapeHtml(text) {
    const div = document.createElement('div');
    div.appendChild(document.createTextNode(text || ''));
    return div.innerHTML;
  }

  window.BookBridgeNotif = { showToast: showToastNotification };
})();
