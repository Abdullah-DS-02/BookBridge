// ============================================
// BookBridge — Main JavaScript
// ============================================

document.addEventListener('DOMContentLoaded', () => {
  initNavbar();
  initAnimations();
  initToasts();
  initCounters();
  initUploadZone();
  initBorrowCountdown();
  initScrollEffects();
  initNewsletter();
});

// ── Navbar ──────────────────────────────────
function initNavbar() {
  const nav = document.getElementById('mainNav');
  if (!nav) return;

  const handleScroll = () => {
    nav.classList.toggle('scrolled', window.scrollY > 20);
  };

  window.addEventListener('scroll', handleScroll, { passive: true });
  handleScroll();
}

// ── GSAP Animations ─────────────────────────
function initAnimations() {
  if (typeof gsap === 'undefined') return;
  gsap.registerPlugin(ScrollTrigger);

  // Hero animation
  const hero = document.querySelector('.hero-section');
  if (hero) {
    const tl = gsap.timeline({ defaults: { ease: 'power3.out' } });
    tl.from('.hero-badge', { opacity: 0, y: 20, duration: 0.6 })
      .from('.hero-title', { opacity: 0, y: 30, duration: 0.8 }, '-=0.3')
      .from('.hero-desc', { opacity: 0, y: 20, duration: 0.6 }, '-=0.5')
      .from('.hero-cta', { opacity: 0, y: 20, duration: 0.5 }, '-=0.4')
      .from('.hero-stats', { opacity: 0, y: 16, duration: 0.5 }, '-=0.3')
      .from('.floating-book', { opacity: 0, y: 40, stagger: 0.2, duration: 0.8 }, '-=0.6');
  }

  // Scroll reveal
  gsap.utils.toArray('[data-reveal]').forEach((el, i) => {
    gsap.from(el, {
      scrollTrigger: { trigger: el, start: 'top 85%', once: true },
      opacity: 0, y: 30, duration: 0.7,
      delay: i * 0.05,
      ease: 'power2.out'
    });
  });

  // Book cards stagger
  gsap.utils.toArray('.book-card').forEach((card, i) => {
    gsap.from(card, {
      scrollTrigger: { trigger: card, start: 'top 90%', once: true },
      opacity: 0, y: 24, scale: 0.97, duration: 0.5,
      delay: (i % 4) * 0.08,
      ease: 'back.out(1.2)'
    });
  });

  // Stats strip
  gsap.utils.toArray('.stat-item').forEach((el, i) => {
    gsap.from(el, {
      scrollTrigger: { trigger: el, start: 'top 85%', once: true },
      opacity: 0, y: 20, duration: 0.6, delay: i * 0.1
    });
  });

  // Section headers
  gsap.utils.toArray('.section-title').forEach(el => {
    gsap.from(el, {
      scrollTrigger: { trigger: el, start: 'top 85%', once: true },
      opacity: 0, y: 20, duration: 0.7, ease: 'power2.out'
    });
  });
}

// ── Scroll Effects ───────────────────────────
function initScrollEffects() {
  const observer = new IntersectionObserver((entries) => {
    entries.forEach(e => { if (e.isIntersecting) e.target.classList.add('visible'); });
  }, { threshold: 0.1 });

  document.querySelectorAll('.fade-in').forEach(el => observer.observe(el));
}

// ── Toast Auto-dismiss ────────────────────── 
function initToasts() {
  document.querySelectorAll('.bb-toast').forEach(toast => {
    setTimeout(() => {
      toast.style.transition = 'opacity 0.4s, transform 0.4s';
      toast.style.opacity = '0';
      toast.style.transform = 'translateX(100%)';
      setTimeout(() => toast.remove(), 400);
    }, 4000);
  });
}

// ── Animated Counters ─────────────────────── 
function initCounters() {
  const counters = document.querySelectorAll('[data-counter]');
  if (!counters.length) return;

  const observer = new IntersectionObserver((entries) => {
    entries.forEach(entry => {
      if (entry.isIntersecting) {
        const el = entry.target;
        const target = parseInt(el.dataset.counter);
        const duration = 2000;
        const step = target / (duration / 16);
        let current = 0;

        const timer = setInterval(() => {
          current += step;
          if (current >= target) { current = target; clearInterval(timer); }
          el.textContent = Math.floor(current).toLocaleString();
        }, 16);

        observer.unobserve(el);
      }
    });
  }, { threshold: 0.5 });

  counters.forEach(c => observer.observe(c));
}

// ── Book Image Upload Zone ────────────────── 
function initUploadZone() {
  const zone = document.getElementById('uploadZone');
  const fileInput = document.getElementById('bookImages');
  const preview = document.getElementById('imagePreview');

  if (!zone || !fileInput) return;

  zone.addEventListener('click', () => fileInput.click());

  zone.addEventListener('dragover', e => {
    e.preventDefault();
    zone.classList.add('drag-over');
  });

  zone.addEventListener('dragleave', () => zone.classList.remove('drag-over'));

  zone.addEventListener('drop', e => {
    e.preventDefault();
    zone.classList.remove('drag-over');
    handleFiles(e.dataTransfer.files);
  });

  fileInput.addEventListener('change', () => handleFiles(fileInput.files));

  function handleFiles(files) {
    if (!preview) return;
    preview.innerHTML = '';

    Array.from(files).slice(0, 5).forEach((file, i) => {
      const reader = new FileReader();
      reader.onload = e => {
        const div = document.createElement('div');
        div.className = `preview-thumb ${i === 0 ? 'primary' : ''}`;
        div.innerHTML = `
          <img src="${e.target.result}" alt="Preview ${i + 1}">
          <span class="remove-img" onclick="this.parentElement.remove()">✕</span>
          ${i === 0 ? '<span class="primary-label" style="position:absolute;bottom:4px;left:4px;background:var(--primary);color:white;font-size:0.6rem;padding:2px 6px;border-radius:4px;">Primary</span>' : ''}
        `;
        preview.appendChild(div);
      };
      reader.readAsDataURL(file);
    });
  }
}

// ── Borrow Countdown Timer ────────────────── 
function initBorrowCountdown() {
  const countdowns = document.querySelectorAll('[data-due-date]');
  countdowns.forEach(el => {
    const dueDate = new Date(el.dataset.dueDate);
    const daysEl = el.querySelector('[data-days]');
    const hoursEl = el.querySelector('[data-hours]');
    const minsEl = el.querySelector('[data-mins]');

    function update() {
      const now = new Date();
      const diff = dueDate - now;

      if (diff <= 0) {
        el.classList.add('overdue');
        if (daysEl) daysEl.textContent = '00';
        if (hoursEl) hoursEl.textContent = '00';
        if (minsEl) minsEl.textContent = '00';
        return;
      }

      const days = Math.floor(diff / (1000 * 60 * 60 * 24));
      const hours = Math.floor((diff % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
      const mins = Math.floor((diff % (1000 * 60 * 60)) / (1000 * 60));

      if (daysEl) daysEl.textContent = String(days).padStart(2, '0');
      if (hoursEl) hoursEl.textContent = String(hours).padStart(2, '0');
      if (minsEl) minsEl.textContent = String(mins).padStart(2, '0');
    }

    update();
    setInterval(update, 60000);
  });
}

// ── Rating Stars Interactive ──────────────── 
function initStarRating() {
  const containers = document.querySelectorAll('.star-rating-input');
  containers.forEach(container => {
    const stars = container.querySelectorAll('[data-star]');
    const input = container.querySelector('input[type=hidden]');

    stars.forEach(star => {
      star.addEventListener('mouseenter', () => {
        const val = parseInt(star.dataset.star);
        stars.forEach(s => s.classList.toggle('active', parseInt(s.dataset.star) <= val));
      });

      star.addEventListener('click', () => {
        const val = parseInt(star.dataset.star);
        if (input) input.value = val;
        stars.forEach(s => s.classList.toggle('selected', parseInt(s.dataset.star) <= val));
      });
    });

    container.addEventListener('mouseleave', () => {
      const selected = parseInt(input?.value || 0);
      stars.forEach(s => {
        s.classList.remove('active');
        s.classList.toggle('selected', parseInt(s.dataset.star) <= selected);
      });
    });
  });
}

// ── Confirm Dialogs ───────────────────────── 
document.querySelectorAll('[data-confirm]').forEach(el => {
  el.addEventListener('click', e => {
    if (!confirm(el.dataset.confirm)) e.preventDefault();
  });
});

// ── Image Gallery Lightbox ────────────────── 
function openLightbox(src) {
  const lb = document.createElement('div');
  lb.style.cssText = `
    position:fixed;inset:0;background:rgba(0,0,0,0.9);z-index:9999;
    display:flex;align-items:center;justify-content:center;cursor:pointer;
  `;
  lb.innerHTML = `<img src="${src}" style="max-width:90vw;max-height:90vh;object-fit:contain;border-radius:12px;" />`;
  lb.addEventListener('click', () => lb.remove());
  document.body.appendChild(lb);

  gsap.from(lb.querySelector('img'), { scale: 0.8, opacity: 0, duration: 0.3, ease: 'back.out' });
}

document.querySelectorAll('[data-lightbox]').forEach(el => {
  el.style.cursor = 'zoom-in';
  el.addEventListener('click', () => openLightbox(el.src || el.dataset.lightbox));
});

// ── Mobile Sidebar Toggle ─────────────────── 
const mobileMenuBtn = document.getElementById('mobileSidebarBtn');
const sidebar = document.querySelector('.dashboard-sidebar');

if (mobileMenuBtn && sidebar) {
  mobileMenuBtn.addEventListener('click', () => {
    sidebar.style.display = sidebar.style.display === 'block' ? '' : 'block';
  });
}

// ── Search Typeahead ──────────────────────── 
const searchInput = document.querySelector('.search-input');
if (searchInput) {
  let searchTimer;
  searchInput.addEventListener('input', e => {
    clearTimeout(searchTimer);
    const q = e.target.value.trim();
    if (q.length < 2) return;

    searchTimer = setTimeout(async () => {
      try {
        const res = await fetch(`/Books/Search?q=${encodeURIComponent(q)}&ajax=1`);
        // Handle suggestions if endpoint returns JSON
      } catch {}
    }, 300);
  });
}

// ── Expose globals ─────────────────────────── 
window.openLightbox = openLightbox;
window.initStarRating = initStarRating;

document.addEventListener('DOMContentLoaded', () => {
  initStarRating();
  initThemeToggle();
  initMouseFollower();
  init3DTilt();
});

// ── Theme Toggle ──────────────────────────── 
function initThemeToggle() {
  // Use a new key to force a reset for returning users
  let currentTheme = localStorage.getItem('theme-preference');
  if (!currentTheme) {
    currentTheme = 'light'; // Default to light
    localStorage.setItem('theme-preference', currentTheme);
  }
  
  document.documentElement.setAttribute('data-theme', currentTheme);

  const toggleBtn = document.getElementById('themeToggleBtn');
  if (!toggleBtn) return;
  
  const icon = toggleBtn.querySelector('i');
  updateIcon(currentTheme);

  toggleBtn.addEventListener('click', () => {
    currentTheme = currentTheme === 'dark' ? 'light' : 'dark';
    document.documentElement.setAttribute('data-theme', currentTheme);
    localStorage.setItem('theme-preference', currentTheme);
    updateIcon(currentTheme);
  });

  function updateIcon(theme) {
    // Interchanged capabilities: Icon reflects the CURRENT state
    if (theme === 'dark') {
      icon.className = 'bi bi-moon-stars-fill text-primary'; // Currently dark -> Moon
    } else {
      icon.className = 'bi bi-sun-fill text-warning'; // Currently light -> Sun
    }
  }
}

// ── Ambient Mouse Follower ────────────────── 
function initMouseFollower() {
  const follower = document.getElementById('mouseFollower');
  if (!follower) return;

  let mouseX = 0, mouseY = 0;
  let currentX = 0, currentY = 0;

  document.addEventListener('mousemove', (e) => {
    mouseX = e.clientX;
    mouseY = e.clientY;
    // Show orb on first move
    if (follower.style.opacity !== '1') {
      follower.style.opacity = '1';
    }
  });

  document.addEventListener('mouseleave', () => {
    follower.style.opacity = '0';
  });

  document.addEventListener('mouseenter', () => {
    follower.style.opacity = '1';
  });

  function animateFollower() {
    // Smooth trailing effect
    currentX += (mouseX - currentX) * 0.1;
    currentY += (mouseY - currentY) * 0.1;
    
    follower.style.transform = `translate(calc(${currentX}px - 50%), calc(${currentY}px - 50%))`;
    requestAnimationFrame(animateFollower);
  }
  
  animateFollower();
}

// ── 3D Card Tilt Effect ───────────────────── 
function init3DTilt() {
  const cards = document.querySelectorAll('.book-card');
  
  cards.forEach(card => {
    card.addEventListener('mousemove', (e) => {
      const rect = card.getBoundingClientRect();
      const x = e.clientX - rect.left; // x position within the element
      const y = e.clientY - rect.top;  // y position within the element
      
      const centerX = rect.width / 2;
      const centerY = rect.height / 2;
      
      const rotateX = ((y - centerY) / centerY) * -8; // Max rotation 8deg
      const rotateY = ((x - centerX) / centerX) * 8;
      
      card.style.transform = `perspective(1000px) scale(1.02) rotateX(${rotateX}deg) rotateY(${rotateY}deg)`;
      card.style.transition = 'none';
    });
    
    card.addEventListener('mouseleave', () => {
      card.style.transform = `perspective(1000px) scale(1) rotateX(0deg) rotateY(0deg)`;
      card.style.transition = 'transform 0.5s ease';
    });
    
    card.addEventListener('mouseenter', () => {
      card.style.transition = 'transform 0.1s ease';
    });
  });
}

// ── Newsletter Subscription ─────────────────
function initNewsletter() {
  const emailInput = document.getElementById('newsletterEmail');
  const btn = document.getElementById('newsletterSubscribeBtn');
  if (emailInput && btn) {
    btn.addEventListener('click', async () => {
      const email = emailInput.value.trim();
      const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
      if (!email || !emailRegex.test(email)) {
        showDynamicToast('Invalid Email', 'Please enter a valid email address.', 'error');
        return;
      }
      
      try {
        btn.disabled = true;
        btn.textContent = 'Subscribing...';
        
        const response = await fetch('/Home/SubscribeNewsletter', {
          method: 'POST',
          headers: {
            'Content-Type': 'application/x-www-form-urlencoded'
          },
          body: `email=${encodeURIComponent(email)}`
        });
        
        const data = await response.json();
        if (data.success) {
          showDynamicToast('Subscribed!', data.message, 'success');
          emailInput.value = '';
        } else {
          showDynamicToast('Error', data.message, 'error');
        }
      } catch (err) {
        showDynamicToast('Error', 'An error occurred. Please try again.', 'error');
      } finally {
        btn.disabled = false;
        btn.textContent = 'Subscribe';
      }
    });
  }
}

function showDynamicToast(title, message, type) {
  const container = document.querySelector('.toast-container');
  if (!container) return;
  
  const toast = document.createElement('div');
  toast.className = 'toast bb-toast show';
  toast.setAttribute('role', 'alert');
  
  const isSuccess = type === 'success';
  const iconClass = isSuccess ? 'bi-check-circle-fill text-success' : 'bi-x-circle-fill text-danger';
  
  toast.innerHTML = `
    <div class="toast-header border-0">
      <i class="bi ${iconClass} me-2"></i>
      <strong class="me-auto">${title}</strong>
      <button type="button" class="btn-close" data-bs-dismiss="toast" aria-label="Close"></button>
    </div>
    <div class="toast-body">${message}</div>
  `;
  
  container.appendChild(toast);
  
  // Auto dismiss
  setTimeout(() => {
    toast.style.transition = 'opacity 0.4s, transform 0.4s';
    toast.style.opacity = '0';
    toast.style.transform = 'translateX(100%)';
    setTimeout(() => toast.remove(), 400);
  }, 4000);
  
  // Close button functionality
  toast.querySelector('.btn-close').addEventListener('click', () => {
    toast.remove();
  });
}

// Expose showDynamicToast globally
window.showDynamicToast = showDynamicToast;
