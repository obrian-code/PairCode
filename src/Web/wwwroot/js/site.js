document.addEventListener("DOMContentLoaded", () => {
  initLoadingButtons();
  initConfirmDialogs();
});

function initLoadingButtons() {
  document.querySelectorAll("form").forEach(form => {
    form.addEventListener("submit", () => {
      const btn = form.querySelector("[type=submit]");
      if (btn && !btn.classList.contains("no-loading")) {
        btn.classList.add("btn-loading");
      }
    });
  });
}

function initConfirmDialogs() {
  document.querySelectorAll("[data-confirm]").forEach(el => {
    el.addEventListener("click", e => {
      if (!confirm(el.dataset.confirm)) {
        e.preventDefault();
      }
    });
  });
}

function showToast(message, type = "success") {
  const container = document.getElementById("toastContainer");
  if (!container) return;

  const colors = {
    success: "bg-success text-white",
    error: "bg-danger text-white",
    warning: "bg-warning text-dark",
    info: "bg-info text-dark"
  };

  const toast = document.createElement("div");
  toast.className = `toast align-items-center ${colors[type] || colors.info} border-0 show`;
  toast.role = "alert";
  toast.ariaLive = "assertive";
  toast.ariaAtomic = "true";
  toast.innerHTML = `
    <div class="d-flex">
      <div class="toast-body">${message}</div>
      <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button>
    </div>
  `;

  container.appendChild(toast);
  setTimeout(() => {
    toast.classList.remove("show");
    setTimeout(() => toast.remove(), 300);
  }, 4000);
}

function getInitials(name) {
  if (!name) return "?";
  return name.split(" ").map(w => w[0]).join("").toUpperCase().slice(0, 2);
}

const avatarColors = [
  "#4361ee", "#3a0ca3", "#7209b7", "#f72585",
  "#4cc9f0", "#4895ef", "#560bad", "#b5179e",
  "#06d6a0", "#118ab2", "#073b4c", "#ef476f",
  "#ffd166", "#06d6a0", "#26547c", "#ff5714"
];

function getAvatarColor(userId) {
  let hash = 0;
  for (let i = 0; i < userId.length; i++) {
    hash = ((hash << 5) - hash) + userId.charCodeAt(i);
    hash |= 0;
  }
  return avatarColors[Math.abs(hash) % avatarColors.length];
}
