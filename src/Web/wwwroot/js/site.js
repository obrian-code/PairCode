document.addEventListener("DOMContentLoaded", () => {
  initLoadingButtons();
  initConfirmDialogs();
  initFormValidation();
  initPasswordStrength();
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

function getAvatarTextColor(bgColor) {
  const r = parseInt(bgColor.slice(1,3), 16);
  const g = parseInt(bgColor.slice(3,5), 16);
  const b = parseInt(bgColor.slice(5,7), 16);
  const luminance = (0.299 * r + 0.587 * g + 0.114 * b) / 255;
  return luminance > 0.5 ? "#000000" : "#ffffff";
}

function initFormValidation() {
  document.querySelectorAll("form[novalidate]").forEach(form => {
    form.addEventListener("submit", function(e) {
      if (!form.checkValidity()) {
        e.preventDefault();
        e.stopPropagation();
      }
      form.classList.add("was-validated");
    });

    form.querySelectorAll("input, select, textarea").forEach(input => {
      input.addEventListener("blur", function() {
        validateField(this);
      });

      input.addEventListener("input", function() {
        if (this.classList.contains("is-invalid") || this.classList.contains("is-valid")) {
          validateField(this);
        }
      });
    });
  });

  document.querySelectorAll("form:not([novalidate])").forEach(form => {
    form.setAttribute("novalidate", "");
    form.addEventListener("submit", function(e) {
      if (!form.checkValidity()) {
        e.preventDefault();
        e.stopPropagation();
      }
    });

    form.querySelectorAll("input, select, textarea").forEach(input => {
      input.addEventListener("blur", function() {
        validateField(this);
      });

      input.addEventListener("input", function() {
        if (this.classList.contains("is-invalid") || this.classList.contains("is-valid")) {
          validateField(this);
        }
      });
    });
  });
}

function validateField(field) {
  field.classList.remove("is-valid", "is-invalid");
  
  if (field.required && !field.value.trim()) {
    field.classList.add("is-invalid");
    return false;
  }

  if (field.type === "email" && field.value) {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(field.value)) {
      field.classList.add("is-invalid");
      return false;
    }
  }

  if (field.minLength > 0 && field.value.length < field.minLength) {
    field.classList.add("is-invalid");
    return false;
  }

  if (field.value) {
    field.classList.add("is-valid");
  }
  return true;
}

function initPasswordStrength() {
  document.querySelectorAll("input[type='password']").forEach(input => {
    if (input.name !== "NewPassword" && input.name !== "Password") return;

    const container = document.createElement("div");
    container.className = "password-strength";
    container.innerHTML = `
      <div class="progress">
        <div class="progress-bar" role="progressbar" style="width: 0%"></div>
      </div>
      <small class="password-strength-text text-muted"></small>
    `;
    input.parentNode.insertBefore(container, input.nextSibling);

    const progressBar = container.querySelector(".progress-bar");
    const strengthText = container.querySelector(".password-strength-text");

    input.addEventListener("input", function() {
      const password = this.value;
      const strength = calculatePasswordStrength(password);
      
      progressBar.style.width = strength.score + "%";
      progressBar.className = "progress-bar " + strength.className;
      strengthText.textContent = password.length > 0 ? strength.text : "";
      strengthText.className = "password-strength-text " + strength.textClass;
    });
  });
}

function calculatePasswordStrength(password) {
  let score = 0;
  
  if (password.length >= 8) score += 25;
  if (password.length >= 12) score += 10;
  if (/[a-z]/.test(password)) score += 15;
  if (/[A-Z]/.test(password)) score += 20;
  if (/[0-9]/.test(password)) score += 15;
  if (/[^a-zA-Z0-9]/.test(password)) score += 15;

  if (score < 30) {
    return { score, className: "bg-danger", text: "Débil", textClass: "text-danger" };
  } else if (score < 60) {
    return { score, className: "bg-warning", text: "Media", textClass: "text-warning" };
  } else if (score < 80) {
    return { score, className: "bg-info", text: "Buena", textClass: "text-info" };
  } else {
    return { score, className: "bg-success", text: "Fuerte", textClass: "text-success" };
  }
}
