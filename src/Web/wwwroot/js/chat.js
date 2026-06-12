const chatConnection = new signalR.HubConnectionBuilder()
  .withUrl("/hubs/chat")
  .build();

let typingTimeout;

chatConnection.on("NewMessage", (message) => {
  const messagesDiv = document.getElementById("messages");
  const isOwn = message.user.userId === currentUserId;

  const msgDiv = document.createElement("div");
  msgDiv.className = "chat-message";

  const avatar = document.createElement("span");
  avatar.className = "avatar avatar-sm";
  avatar.style.backgroundColor = getAvatarColor(message.user.userId || message.userId);
  avatar.textContent = getInitials(message.user.name);
  msgDiv.appendChild(avatar);

  const content = document.createElement("div");
  content.className = "msg-content";
  content.innerHTML = `
    <strong>${escHtml(message.user.name)}</strong>
    ${escHtml(message.message)}
    <small class="text-muted">${new Date(message.sentAt).toLocaleTimeString()}</small>
  `;
  msgDiv.appendChild(content);

  messagesDiv.appendChild(msgDiv);
  messagesDiv.scrollTop = messagesDiv.scrollHeight;

  // Update participants if new user appears
  updateParticipantStatus(message.user.name, message.userId, "online");
});

chatConnection.on("UserJoined", (userName) => {
  const messagesDiv = document.getElementById("messages");
  const el = document.createElement("div");
  el.className = "chat-system";
  el.innerHTML = `<em>${escHtml(userName)} joined the room</em>`;
  messagesDiv.appendChild(el);
  messagesDiv.scrollTop = messagesDiv.scrollHeight;
});

chatConnection.on("UserLeft", (userName) => {
  const messagesDiv = document.getElementById("messages");
  const el = document.createElement("div");
  el.className = "chat-system";
  el.innerHTML = `<em>${escHtml(userName)} left the room</em>`;
  messagesDiv.appendChild(el);
  messagesDiv.scrollTop = messagesDiv.scrollHeight;
});

chatConnection.on("UserTyping", (userName, userId) => {
  const indicator = document.getElementById("typingIndicator");
  const userSpan = document.getElementById("typingUser");
  if (indicator && userSpan) {
    userSpan.textContent = `${escHtml(userName)} is typing`;
    indicator.classList.add("visible");
  }
});

chatConnection.on("UserStoppedTyping", (userId) => {
  const indicator = document.getElementById("typingIndicator");
  if (indicator) {
    indicator.classList.remove("visible");
  }
});

chatConnection.start().then(() => {
  chatConnection.invoke("JoinRoom", roomId);
});

document.getElementById("sendButton").addEventListener("click", () => {
  sendMessage();
});

document.getElementById("messageInput").addEventListener("keypress", (e) => {
  if (e.key === "Enter") {
    sendMessage();
  }
});

document.getElementById("messageInput").addEventListener("input", () => {
  chatConnection.invoke("NotifyTyping", roomId);
  clearTimeout(typingTimeout);
  typingTimeout = setTimeout(() => {
    chatConnection.invoke("NotifyStoppedTyping", roomId);
  }, 2000);
});

function sendMessage() {
  const input = document.getElementById("messageInput");
  const message = input.value.trim();
  if (!message) return;
  chatConnection.invoke("SendMessage", roomId, message).catch(err => {
    showToast("Failed to send message", "error");
  });
  input.value = "";
}

function updateParticipantStatus(name, userId, status) {
  document.querySelectorAll("#participantsList .list-group-item, #participantsListMobile .list-group-item").forEach(item => {
    if (item.textContent.includes(name)) {
      const badge = item.querySelector(".badge");
      if (badge) {
        badge.className = `ms-auto badge bg-${status === "online" ? "success" : "secondary"}`;
        badge.textContent = status === "online" ? "Online" : "Left";
      }
    }
  });
}

function escHtml(str) {
  if (!str) return "";
  const div = document.createElement("div");
  div.textContent = str;
  return div.innerHTML;
}
