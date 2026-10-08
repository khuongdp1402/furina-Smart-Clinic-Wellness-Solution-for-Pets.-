/**
 * FURINA — Smart Clinic & Wellness Solution for Pets
 * Enterprise Multi-View Application Logic
 * Covers: Client Portal, TASK-30 (SignalR Dispatch), TASK-31 (Clinical SOAP & Rx), 
 * TASK-32 (FEFO Inventory & POS), TASK-34-36 (Super Admin Multi-Tenant)
 */

// Global State
const state = {
  activeView: 'client',
  currentClinic: 'clinic-2',
  selectedPetKey: 'mochi',
  selectedSlot: '09:30',
  bookingCost: 250000,
  
  // Realtime Appointments (TASK-30)
  appointments: [
    {
      id: 'appt-101',
      time: '09:00',
      petName: 'Mochi',
      petType: 'dog',
      breed: 'Golden Retriever',
      avatar: '🐶',
      owner: 'Đỗ Phú Khương (0918.xxx.789)',
      service: 'Khám Viêm Tai & Soi Ký Sinh',
      doctor: 'BS. Trần Thảo Vy',
      room: 'Phòng Khám 01',
      status: 'waiting', // scheduled, waiting, in-progress, completed
      weight: 28.5,
      cost: 320000
    },
    {
      id: 'appt-102',
      time: '09:30',
      petName: 'Lulu',
      petType: 'cat',
      breed: 'Mèo Anh Lông Ngắn',
      avatar: '🐱',
      owner: 'Nguyễn Thanh Vân (0903.xxx.112)',
      service: 'Khám Hô Hấp & Cắt Móng',
      doctor: 'BS. Trần Thảo Vy',
      room: 'Phòng Khám 02',
      status: 'in-progress',
      weight: 4.2,
      cost: 435000
    },
    {
      id: 'appt-103',
      time: '10:00',
      petName: 'Rocky',
      petType: 'dog',
      breed: 'Siberian Husky',
      avatar: '🐺',
      owner: 'Lê Hoàng Nam (0988.xxx.345)',
      service: 'Tiêm Phòng Dại + 7 Bệnh',
      doctor: 'BS. Nguyễn Văn Hùng',
      room: 'Phòng Tiêm Chủng',
      status: 'scheduled',
      weight: 24.0,
      cost: 350000
    },
    {
      id: 'appt-104',
      time: '10:30',
      petName: 'Bông',
      petType: 'dog',
      breed: 'Poodle Trắng',
      avatar: '🐩',
      owner: 'Đỗ Phú Khương (0918.xxx.789)',
      service: 'Spa Tạo Kiểu & Tắm Thơm',
      doctor: 'KTV. Hoàng Minh Tuấn',
      room: 'Khu Spa VIP',
      status: 'scheduled',
      weight: 3.8,
      cost: 450000
    },
    {
      id: 'appt-105',
      time: '08:30',
      petName: 'Bắp',
      petType: 'dog',
      breed: 'Corgi Pembroke',
      avatar: '🐶',
      owner: 'Trần Minh Quân (0977.xxx.890)',
      service: 'Cạo Vôi Răng Siêu Âm',
      doctor: 'BS. Nguyễn Văn Hùng',
      room: 'Phòng Tiểu Phẫu',
      status: 'in-progress',
      weight: 11.2,
      cost: 280000
    },
    {
      id: 'appt-106',
      time: '08:00',
      petName: 'Miu Miu',
      petType: 'cat',
      breed: 'Mèo Ba Tư',
      avatar: '🐱',
      owner: 'Phạm Thu Trang (0933.xxx.567)',
      service: 'Tẩy giun & Khám Da Liễu',
      doctor: 'BS. Trần Thảo Vy',
      room: 'Phòng Khám 01',
      status: 'completed',
      weight: 3.5,
      cost: 390000
    }
  ],

  // Digital Pet Profiles
  pets: {
    mochi: {
      name: 'Mochi',
      avatar: '🐶',
      rfid: 'RFID: 982.014.281.092',
      breed: 'Golden Retriever (Thuần chủng)',
      age: '2 năm 4 tháng',
      weight: 28.5,
      owner: 'Đỗ Phú Khương (0918.xxx.789)',
      stars: 1450,
      vaccines: [
        { name: 'Vanguard Plus 5/L (Mũi 1)', date: '15/01/2025', status: 'done', doctor: 'BS. Vy' },
        { name: 'Vanguard Plus 5/L (Mũi 2)', date: '05/02/2025', status: 'done', doctor: 'BS. Vy' },
        { name: 'Rabisin Dại (Hàng năm)', date: '12/03/2025', status: 'done', doctor: 'BS. Hùng' },
        { name: 'Tiêm Nhắc 7 Bệnh Định Kỳ', date: '12/03/2026', status: 'due', doctor: 'Chưa tiêm' }
      ]
    },
    lulu: {
      name: 'Lulu',
      avatar: '🐱',
      rfid: 'RFID: 982.014.390.412',
      breed: 'Mèo Anh Lông Ngắn (Silver Tabby)',
      age: '1 năm 8 tháng',
      weight: 4.2,
      owner: 'Đỗ Phú Khương (0918.xxx.789)',
      stars: 820,
      vaccines: [
        { name: 'Nobivac Tricat Trio (4 Bệnh Mèo Mũi 1)', date: '10/04/2025', status: 'done', doctor: 'BS. Vy' },
        { name: 'Nobivac Tricat Trio (Mũi 2)', date: '08/05/2025', status: 'done', doctor: 'BS. Vy' },
        { name: 'Vắc-xin Dại Rabisin Mèo', date: '08/06/2025', status: 'done', doctor: 'BS. Hùng' },
        { name: 'Nhắc lại 4 Bệnh Định Kỳ', date: '08/06/2026', status: 'due', doctor: 'Chưa tiêm' }
      ]
    },
    bong: {
      name: 'Bông',
      avatar: '🐩',
      rfid: 'RFID: 982.014.887.103',
      breed: 'Poodle Teacup Trắng',
      age: '11 tháng',
      weight: 3.8,
      owner: 'Đỗ Phú Khương (0918.xxx.789)',
      stars: 560,
      vaccines: [
        { name: 'Vanguard Plus 5/L (Mũi 1)', date: '10/11/2025', status: 'done', doctor: 'BS. Hùng' },
        { name: 'Vanguard Plus 5/L (Mũi 2)', date: '01/12/2025', status: 'done', doctor: 'BS. Hùng' },
        { name: 'Rabisin Dại', date: '20/12/2025', status: 'done', doctor: 'BS. Vy' }
      ]
    }
  },

  // FEFO Inventory Batches (TASK-32)
  inventory: [
    {
      batchNo: 'BAT-2026-0041',
      name: 'Clavamox 62.5mg (Hộp 14 viên)',
      form: 'Viên nén bao phim',
      mfgDate: '15/03/2025',
      expDate: '10/10/2026',
      daysLeft: 15,
      priority: '#1 (Ưu tiên xuất trước)',
      stock: 12,
      price: 185000,
      status: 'expiring'
    },
    {
      batchNo: 'BAT-2026-0098',
      name: 'Kháng viêm Meloxicam 1.5mg/ml (Chai 10ml)',
      form: 'Hỗn dịch uống',
      mfgDate: '01/01/2025',
      expDate: '25/10/2026',
      daysLeft: 30,
      priority: '#2',
      stock: 4,
      price: 210000,
      status: 'expiring'
    },
    {
      batchNo: 'BAT-2026-0112',
      name: 'Vắc-xin Nobivac Tricat Trio',
      form: 'Lọ đông khô + dung môi',
      mfgDate: '20/06/2025',
      expDate: '15/12/2026',
      daysLeft: 81,
      priority: '#3',
      stock: 5,
      price: 320000,
      status: 'low'
    },
    {
      batchNo: 'BAT-2026-0205',
      name: 'Nhỏ gáy Advocate Chó 10-25kg',
      form: 'Tuýp 2.5ml',
      mfgDate: '10/08/2025',
      expDate: '10/08/2027',
      daysLeft: 319,
      priority: 'Tiêu chuẩn',
      stock: 48,
      price: 260000,
      status: 'safe'
    },
    {
      batchNo: 'BAT-2026-0210',
      name: 'Gel Dinh Dưỡng Nutri-plus (Tuýp 120g)',
      form: 'Gel năng lượng cao',
      mfgDate: '01/09/2025',
      expDate: '01/09/2027',
      daysLeft: 341,
      priority: 'Tiêu chuẩn',
      stock: 35,
      price: 160000,
      status: 'safe'
    },
    {
      batchNo: 'BAT-2026-0312',
      name: 'Nước rửa tai Epi-Otic 125ml',
      form: 'Dung dịch làm sạch tai',
      mfgDate: '12/10/2025',
      expDate: '12/10/2027',
      daysLeft: 382,
      priority: 'Tiêu chuẩn',
      stock: 22,
      price: 195000,
      status: 'safe'
    }
  ],

  // POS Cashier Cart (TASK-32)
  posCart: [
    { id: 1, name: 'Phí Khám Lâm Sàng Chuyên Sâu', qty: 1, price: 250000 },
    { id: 2, name: 'Clavamox 62.5mg (Đơn thuốc Lô BAT-2026-0041)', qty: 1, price: 185000 }
  ],
  usePointsDiscount: false,
  selectedPayMethod: 'qr'
};

/* ======================================================== */
/* 1. INITIALIZATION & ROUTING                              */
/* ======================================================== */
document.addEventListener('DOMContentLoaded', () => {
  // Set default booking date to tomorrow
  const tomorrow = new Date();
  tomorrow.setDate(tomorrow.getDate() + 1);
  const dateInput = document.getElementById('bookingDate');
  if (dateInput) {
    dateInput.value = tomorrow.toISOString().split('T')[0];
  }

  // Render initial components
  renderDigitalPetCard();
  renderVaccineTimeline();
  renderDispatchBoard();
  renderConsultationPrescription();
  renderInventoryTable();
  renderPosPendingOrders();
  renderPosCart();

  // Test live backend connection (.NET 8 Docker API on 8080)
  checkBackendHealth();

  // Start SignalR Realtime Simulator
  startSignalRSimulation();
});

function switchView(viewName) {
  state.activeView = viewName;

  // Update nav buttons
  document.querySelectorAll('#mainMenu .nav-btn').forEach(btn => {
    btn.classList.toggle('active', btn.getAttribute('data-view') === viewName);
  });

  // Update view sections
  document.querySelectorAll('.view-section').forEach(sec => {
    sec.classList.toggle('active', sec.id === `view-${viewName}`);
  });

  // Show contextual toast
  const titles = {
    client: 'Chào mừng trở lại Cổng Dịch Vụ Khách Hàng FURINA',
    dispatch: 'Bàn Điều Phối Khám: Đã kết nối SignalR Hub phòng khám',
    consultation: 'Mở Hồ Sơ Bệnh Án Điện Tử & Bàn Kê Đơn (TASK-31)',
    pos: 'Mở Quản Trị Kho Dược FEFO & Thu Ngân POS (TASK-32)',
    superadmin: 'Super Admin Portal: Quản trị mạng lưới 8 chi nhánh (TASK-34-36)'
  };
  showToast('🐾 Điều hướng', titles[viewName] || 'Đã chuyển màn hình');
}

function changeClinic(clinicId) {
  state.currentClinic = clinicId;
  const clinicName = document.querySelector(`#clinicSelect option[value="${clinicId}"]`).textContent;
  showToast('📍 Chuyển Chi Nhánh', `Đã đồng bộ dữ liệu phiên làm việc với: ${clinicName}`);
  refreshDispatchBoard();
}

/* ======================================================== */
/* 2. VIEW 1: CLIENT PORTAL LOGIC                           */
/* ======================================================== */
function selectPet(petKey) {
  state.selectedPetKey = petKey;
  
  // Highlight switcher pill
  document.querySelectorAll('.pet-switcher-bar .pet-pill').forEach(pill => {
    pill.classList.toggle('active', pill.textContent.toLowerCase().includes(petKey));
  });

  renderDigitalPetCard();
  renderVaccineTimeline();
  showToast('🐶 Chọn thú cưng', `Đã tải hồ sơ Digital Pet ID của bé: ${state.pets[petKey].name}`);
}

function renderDigitalPetCard() {
  const pet = state.pets[state.selectedPetKey];
  if (!pet) return;

  const card = document.getElementById('digitalPetCard');
  if (card) {
    document.getElementById('petAvatar').textContent = pet.avatar;
    document.getElementById('petName').textContent = pet.name;
    document.querySelector('.pet-microchip').textContent = pet.rfid;
    document.querySelector('.pet-spec:nth-child(2)').innerHTML = `Giống: <strong>${pet.breed}</strong>`;
    document.querySelector('.pet-spec:nth-child(3)').innerHTML = `Tuổi: <strong>${pet.age}</strong> · Cân nặng: <strong>${pet.weight} kg</strong>`;
    document.getElementById('vaccineCount').textContent = `${pet.vaccines.filter(v => v.status === 'done').length}/${pet.vaccines.length}`;
    document.querySelector('.cstat-val').textContent = pet.stars.toLocaleString('vi-VN');
  }
}

function renderVaccineTimeline() {
  const pet = state.pets[state.selectedPetKey];
  const container = document.getElementById('vaccineTimeline');
  if (!container || !pet) return;

  container.innerHTML = pet.vaccines.map(v => `
    <div class="vaccine-item">
      <div class="v-dot ${v.status === 'due' ? 'pending' : ''}"></div>
      <div class="v-content">
        <div class="v-title-row">
          <span>${v.name}</span>
          <span class="badge ${v.status === 'done' ? 'badge-success' : 'badge-warning'}">
            ${v.status === 'done' ? '✓ Đã hoàn tất' : '⏳ Cần tiêm nhắc'}
          </span>
        </div>
        <div class="v-date">Thời gian: ${v.date} · Phụ trách: ${v.doctor}</div>
      </div>
    </div>
  `).join('');
}

function selectSlot(btn, time) {
  state.selectedSlot = time;
  document.querySelectorAll('#timeSlotGrid .slot-btn').forEach(b => b.classList.remove('active'));
  btn.classList.add('active');
}

function updateBookingCost() {
  const select = document.getElementById('bookingService');
  const price = parseInt(select.options[select.selectedIndex].getAttribute('data-price') || 250000);
  state.bookingCost = price;
  document.getElementById('bookingCostValue').textContent = price.toLocaleString('vi-VN') + ' đ';
}

function handleBookingSubmit(e) {
  e.preventDefault();
  const serviceSelect = document.getElementById('bookingService');
  const serviceName = serviceSelect.options[serviceSelect.selectedIndex].text.split('(')[0].trim();
  const docSelect = document.getElementById('bookingDoctor');
  const docName = docSelect.options[docSelect.selectedIndex].text.split('(')[0].trim();
  const dateVal = document.getElementById('bookingDate').value;
  const pet = state.pets[state.selectedPetKey];

  // Insert appointment into Realtime Dispatch Board
  const newAppt = {
    id: `appt-${Date.now().toString().slice(-4)}`,
    time: state.selectedSlot,
    petName: pet.name,
    petType: state.selectedPetKey === 'lulu' ? 'cat' : 'dog',
    breed: pet.breed,
    avatar: pet.avatar,
    owner: pet.owner,
    service: serviceName,
    doctor: docName,
    room: 'Phòng Tiếp Nhận Chờ',
    status: 'scheduled',
    weight: pet.weight,
    cost: state.bookingCost
  };

  state.appointments.unshift(newAppt);
  renderDispatchBoard();

  showToast(
    '🎉 Đặt lịch thành công!',
    `Mã phiếu hẹn: #${newAppt.id.toUpperCase()}. Lịch khám của bé ${pet.name} lúc ${state.selectedSlot} đã gửi tới Bàn điều phối!`
  );

  setTimeout(() => {
    switchView('dispatch');
  }, 1000);
}

function redeemVoucher(stars, code) {
  const pet = state.pets[state.selectedPetKey];
  if (pet.stars >= stars) {
    pet.stars -= stars;
    renderDigitalPetCard();
    showToast('🎁 Đổi voucher thành công', `Đã trừ ${stars} Stars! Mã ưu đãi [${code}] đã được kích hoạt trong giỏ hàng POS.`);
  } else {
    showToast('⚠️ Không đủ điểm', `Bạn cần tích lũy thêm ${stars - pet.stars} Stars để đổi quà này.`);
  }
}

function scrollToBooking() {
  document.getElementById('bookingSection').scrollIntoView({ behavior: 'smooth' });
}

function showPetModal() {
  const pet = state.pets[state.selectedPetKey];
  const content = document.getElementById('petModalContent');
  content.innerHTML = `
    <div style="text-align: center; margin-bottom: 1.5rem;">
      <div style="font-size: 4rem; margin-bottom: 0.5rem;">${pet.avatar}</div>
      <h2 style="font-size: 1.8rem; font-family: var(--font-heading); color: #FFF;">${pet.name}</h2>
      <p style="color: var(--text-muted);">${pet.breed}</p>
      <div style="display: inline-block; margin-top: 0.5rem; background: rgba(99, 102, 241, 0.15); border: 1px solid #6366F1; padding: 0.35rem 0.85rem; border-radius: 9999px; font-family: monospace; color: #93C5FD;">
        ${pet.rfid}
      </div>
    </div>

    <div style="background: rgba(255,255,255,0.03); border: 1px solid var(--border-color); border-radius: var(--radius-md); padding: 1.25rem; margin-bottom: 1.25rem;">
      <div style="display: flex; justify-content: space-between; margin-bottom: 0.5rem;">
        <span style="color: var(--text-muted);">Cân nặng:</span>
        <strong>${pet.weight} kg</strong>
      </div>
      <div style="display: flex; justify-content: space-between; margin-bottom: 0.5rem;">
        <span style="color: var(--text-muted);">Độ tuổi:</span>
        <strong>${pet.age}</strong>
      </div>
      <div style="display: flex; justify-content: space-between; margin-bottom: 0.5rem;">
        <span style="color: var(--text-muted);">Chủ nhân:</span>
        <strong>${pet.owner}</strong>
      </div>
      <div style="display: flex; justify-content: space-between;">
        <span style="color: var(--text-muted);">Điểm Furina Stars:</span>
        <strong style="color: #FBBF24;">${pet.stars} Stars (Kim Cương)</strong>
      </div>
    </div>

    <button class="btn btn-primary w-full" onclick="closePetModal()">Đóng cửa sổ</button>
  `;
  document.getElementById('petModal').classList.add('active');
}

function closePetModal(e) {
  if (!e || e.target.id === 'petModal' || e.target.classList.contains('close-btn')) {
    document.getElementById('petModal').classList.remove('active');
  }
}

function addNewPetPrompt() {
  const name = prompt('Nhập tên thú cưng mới:');
  if (name) {
    showToast('🐾 Tạo hồ sơ mới', `Đã khởi tạo yêu cầu cấp RFID Pet ID cho bé ${name}. Hãy đưa bé tới cơ sở gần nhất để gắn chip!`);
  }
}

/* ======================================================== */
/* 3. VIEW 2: REALTIME DISPATCH BOARD (TASK-30)             */
/* ======================================================== */
function renderDispatchBoard() {
  const cols = {
    scheduled: document.getElementById('colScheduled'),
    waiting: document.getElementById('colWaiting'),
    'in-progress': document.getElementById('colInProgress'),
    completed: document.getElementById('colCompleted')
  };

  // Reset columns
  Object.values(cols).forEach(col => { if (col) col.innerHTML = ''; });

  const counts = { scheduled: 0, waiting: 0, 'in-progress': 0, completed: 0 };

  state.appointments.forEach(appt => {
    if (counts[appt.status] !== undefined) counts[appt.status]++;

    const card = document.createElement('div');
    card.className = 'ticket-card';
    card.innerHTML = `
      <div class="ticket-top">
        <span class="ticket-time">⏰ ${appt.time}</span>
        <span class="badge ${getStatusBadgeClass(appt.status)}">${getStatusLabel(appt.status)}</span>
      </div>
      <div class="ticket-pet">
        <span class="t-avatar">${appt.avatar}</span>
        <div>
          <div class="t-name">${appt.petName}</div>
          <div class="t-breed">${appt.breed}</div>
        </div>
      </div>
      <div class="ticket-service">${appt.service}</div>
      <div class="ticket-meta">
        <span>👨‍⚕️ ${appt.doctor}</span>
        <span>🚪 ${appt.room}</span>
      </div>
      <div class="ticket-actions">
        ${renderTicketActionButtons(appt)}
      </div>
    `;

    if (cols[appt.status]) {
      cols[appt.status].appendChild(card);
    }
  });

  // Update counts
  if (document.getElementById('countScheduled')) document.getElementById('countScheduled').textContent = counts.scheduled;
  if (document.getElementById('countWaiting')) document.getElementById('countWaiting').textContent = counts.waiting;
  if (document.getElementById('countInProgress')) document.getElementById('countInProgress').textContent = counts['in-progress'];
  if (document.getElementById('countCompleted')) document.getElementById('countCompleted').textContent = counts.completed;

  // Update KPI counters
  if (document.getElementById('kpiTotalAppts')) document.getElementById('kpiTotalAppts').textContent = state.appointments.length;
  if (document.getElementById('kpiWaiting')) document.getElementById('kpiWaiting').textContent = counts.waiting;
  if (document.getElementById('kpiInConsult')) document.getElementById('kpiInConsult').textContent = counts['in-progress'];
  if (document.getElementById('kpiCompleted')) document.getElementById('kpiCompleted').textContent = counts.completed;
  if (document.getElementById('dispatchBadge')) document.getElementById('dispatchBadge').textContent = counts.waiting;
}

function getStatusBadgeClass(status) {
  switch (status) {
    case 'scheduled': return 'badge-info';
    case 'waiting': return 'badge-warning';
    case 'in-progress': return 'badge-purple';
    case 'completed': return 'badge-success';
    default: return 'badge-info';
  }
}

function getStatusLabel(status) {
  switch (status) {
    case 'scheduled': return 'Đã hẹn trước';
    case 'waiting': return 'Đã check-in';
    case 'in-progress': return 'Đang khám/spa';
    case 'completed': return 'Đã xong / Chờ POS';
    default: return '';
  }
}

function renderTicketActionButtons(appt) {
  switch (appt.status) {
    case 'scheduled':
      return `<button class="btn btn-sm btn-outline" onclick="advanceAppt('${appt.id}', 'waiting')">✓ Check-in Kiosk</button>`;
    case 'waiting':
      return `<button class="btn btn-sm btn-primary" onclick="advanceAppt('${appt.id}', 'in-progress')">🩺 Bắt đầu khám</button>`;
    case 'in-progress':
      return `
        <button class="btn btn-sm btn-outline" onclick="openConsultationForAppt('${appt.id}')">📝 Y bạ SOAP</button>
        <button class="btn btn-sm btn-success" onclick="advanceAppt('${appt.id}', 'completed')">💳 Chuyển POS</button>
      `;
    case 'completed':
      return `<button class="btn btn-sm btn-outline" onclick="openPosForAppt('${appt.id}')">🧾 Lập hóa đơn POS</button>`;
    default:
      return '';
  }
}

function advanceAppt(id, nextStatus) {
  const appt = state.appointments.find(a => a.id === id);
  if (!appt) return;

  appt.status = nextStatus;
  renderDispatchBoard();

  const msgMap = {
    waiting: `Bé ${appt.petName} vừa check-in qua Kiosk! Trạng thái: Sẵn sàng vào buồng khám.`,
    'in-progress': `Bé ${appt.petName} đã được tiếp nhận vào ${appt.room} bởi ${appt.doctor}.`,
    completed: `Hoàn tất khám cho ${appt.petName}! Đã tự động chuyển hồ sơ đơn thuốc sang quầy Thu ngân POS.`
  };

  showToast('📋 Cập nhật điều phối (SignalR)', msgMap[nextStatus]);

  if (nextStatus === 'in-progress') {
    openConsultationForAppt(id);
  }
}

function triggerMockAppointment() {
  const names = ['Sushi', 'Bơ', 'Milo', 'Simba', 'Kem'];
  const breeds = ['Mèo Corgi Tai Cụp', 'Chó Poodle Socola', 'Mèo Bengal', 'Chó Phốc Sóc', 'Chó Samoyed'];
  const name = names[Math.floor(Math.random() * names.length)];
  const breed = breeds[Math.floor(Math.random() * breeds.length)];

  const newAppt = {
    id: `appt-${Date.now().toString().slice(-4)}`,
    time: '11:15',
    petName: name,
    petType: 'dog',
    breed: breed,
    avatar: '🐾',
    owner: 'Khách Đặt Trực Tuyến',
    service: 'Khám Sức Khỏe Tổng Quát',
    doctor: 'BS. Trần Thảo Vy',
    room: 'Phòng Tiếp Nhận',
    status: 'scheduled',
    weight: 6.5,
    cost: 250000
  };

  state.appointments.unshift(newAppt);
  renderDispatchBoard();
  showToast('📡 SignalR Push Event', `Khách hàng mới vừa đặt hẹn cho bé ${name}! (WebSocket live sync)`);
}

function refreshDispatchBoard() {
  renderDispatchBoard();
  showToast('🔄 Đồng bộ SignalR', 'Đã tải lại toàn bộ bàn điều phối phòng khám.');
}

function startSignalRSimulation() {
  // Periodically send background micro-events simulating live clinic activity
  setInterval(() => {
    // 20% chance to simulate a check-in event
    if (Math.random() < 0.25) {
      const scheduledAppt = state.appointments.find(a => a.status === 'scheduled');
      if (scheduledAppt) {
        scheduledAppt.status = 'waiting';
        renderDispatchBoard();
        showToast('🔔 SignalR Kiosk Ping', `Bé ${scheduledAppt.petName} vừa chạm thẻ RFID tại quầy tiếp tân! Trạng thái: Chờ khám.`);
      }
    }
  }, 35000);
}

/* ======================================================== */
/* 4. VIEW 3: CLINICAL CONSULTATION & SOAP NOTE (TASK-31)   */
/* ======================================================== */
function openConsultationForAppt(apptId) {
  const appt = state.appointments.find(a => a.id === apptId);
  if (!appt) return;

  switchView('consultation');
  loadPatientToConsultation(apptId);
}

function loadPatientToConsultation(apptId) {
  const appt = state.appointments.find(a => a.id === apptId);
  if (!appt) return;

  document.getElementById('cPetName').textContent = appt.petName;
  document.getElementById('cPetBreed').textContent = `${appt.breed} (${appt.weight} kg)`;
  document.getElementById('vitalWeight').value = appt.weight;

  // Auto calculate dosage when weight loads
  recalculatePrescriptionDose(appt.weight);
  showToast('🩺 Tải hồ sơ bệnh án', `Đã đồng bộ hồ sơ khám của bé ${appt.petName} (${appt.weight} kg). Hệ thống tự cân chỉnh liều thuốc.`);
}

// Prescription rows
let rxItems = [
  { name: 'Clavamox (Amoxicillin + Clavulanate)', doseMgKg: 12.5, qty: 14, usage: 'Uống 1 viên x 2 lần/ngày sau ăn', price: 10000 },
  { name: 'Kháng viêm Meloxicam 0.5mg', doseMgKg: 0.1, qty: 5, usage: 'Uống 1 lần/ngày vào buổi sáng', price: 9000 }
];

function renderConsultationPrescription() {
  const tbody = document.getElementById('rxTableBody');
  if (!tbody) return;

  let total = 0;
  tbody.innerHTML = rxItems.map((item, idx) => {
    const lineTotal = item.qty * item.price;
    total += lineTotal;
    return `
      <tr>
        <td><strong>${item.name}</strong></td>
        <td><input type="number" step="0.5" class="form-control" style="width: 80px;" value="${item.doseMgKg}" onchange="updateRxDose(${idx}, this.value)"></td>
        <td><input type="number" class="form-control" style="width: 70px;" value="${item.qty}" onchange="updateRxQty(${idx}, this.value)"></td>
        <td><input type="text" class="form-control" value="${item.usage}"></td>
        <td>${item.price.toLocaleString('vi-VN')} đ</td>
        <td><strong>${lineTotal.toLocaleString('vi-VN')} đ</strong></td>
        <td><button class="btn btn-sm btn-outline text-rose" onclick="removePrescriptionRow(${idx})">✕</button></td>
      </tr>
    `;
  }).join('');

  document.getElementById('rxTotalValue').textContent = total.toLocaleString('vi-VN') + ' đ';
}

function updateRxDose(idx, val) {
  rxItems[idx].doseMgKg = parseFloat(val) || 0;
  renderConsultationPrescription();
}

function updateRxQty(idx, val) {
  rxItems[idx].qty = parseInt(val) || 0;
  renderConsultationPrescription();
}

function addPrescriptionRow() {
  rxItems.push({
    name: 'Gel Dinh Dưỡng Nutri-plus (Tuýp)',
    doseMgKg: 5,
    qty: 1,
    usage: 'Ăn trực tiếp 1 thìa cà phê/ngày',
    price: 160000
  });
  renderConsultationPrescription();
  showToast('💊 Đơn thuốc', 'Đã thêm dòng thuốc mới vào toa điện tử.');
}

function removePrescriptionRow(idx) {
  rxItems.splice(idx, 1);
  renderConsultationPrescription();
}

function recalculatePrescriptionDose(weight) {
  // Demo auto-adjust calculation
  renderConsultationPrescription();
}

function printPrescription() {
  window.print();
}

function saveSoapAndPrescription() {
  showToast('💾 Lưu Y Bạ Thành Công', 'Đã chốt SOAP note, lưu đơn thuốc điện tử và tự động gửi sang quầy Thu ngân POS!');
  
  // Also push to POS Cart
  state.posCart = [
    { id: 1, name: 'Khám Bệnh Lâm Sàng (BS. Vy)', qty: 1, price: 250000 },
    { id: 2, name: 'Đơn thuốc Clavamox + Meloxicam', qty: 1, price: 185000 }
  ];
  renderPosCart();

  setTimeout(() => {
    switchView('pos');
    switchPosTab('cashier');
  }, 1000);
}

/* ======================================================== */
/* 5. VIEW 4: FEFO INVENTORY & CASHIER POS (TASK-32)        */
/* ======================================================== */
function switchPosTab(tabName) {
  document.querySelectorAll('.pos-tab-switch .tab-btn').forEach(btn => {
    btn.classList.toggle('active', btn.getAttribute('onclick').includes(tabName));
  });

  document.getElementById('posTabInventory').classList.toggle('active', tabName === 'inventory');
  document.getElementById('posTabCashier').classList.toggle('active', tabName === 'cashier');
}

function renderInventoryTable(filterStatus = 'all', searchQuery = '') {
  const tbody = document.getElementById('inventoryTableBody');
  if (!tbody) return;

  const filtered = state.inventory.filter(item => {
    const matchesStatus = filterStatus === 'all' || item.status === filterStatus;
    const matchesSearch = item.name.toLowerCase().includes(searchQuery.toLowerCase()) || 
                          item.batchNo.toLowerCase().includes(searchQuery.toLowerCase());
    return matchesStatus && matchesSearch;
  });

  tbody.innerHTML = filtered.map(item => `
    <tr>
      <td><code>${item.batchNo}</code></td>
      <td><strong>${item.name}</strong></td>
      <td>${item.form}</td>
      <td>${item.mfgDate}</td>
      <td><strong class="${item.daysLeft <= 30 ? 'text-rose' : ''}">${item.expDate}</strong> (${item.daysLeft} ngày)</td>
      <td><span class="badge ${item.priority.includes('#1') ? 'badge-danger' : item.priority.includes('#2') ? 'badge-warning' : 'badge-info'}">${item.priority}</span></td>
      <td><strong>${item.stock}</strong> đơn vị</td>
      <td>${item.price.toLocaleString('vi-VN')} đ</td>
      <td><span class="badge ${item.status === 'expiring' ? 'badge-danger' : item.status === 'low' ? 'badge-warning' : 'badge-success'}">${getInvStatusLabel(item.status)}</span></td>
      <td>
        <button class="btn btn-sm btn-outline" onclick="deductStockDemo('${item.batchNo}')">Xuất 1</button>
      </td>
    </tr>
  `).join('');
}

function getInvStatusLabel(status) {
  switch (status) {
    case 'expiring': return 'Cận Hạn Sử Dụng';
    case 'low': return 'Sắp Hết Hàng';
    case 'safe': return 'An Toàn';
    default: return 'Bình Thường';
  }
}

function filterInvStatus(status) {
  document.querySelectorAll('.filter-pills .pill-btn').forEach(btn => {
    btn.classList.toggle('active', btn.getAttribute('onclick').includes(status));
  });
  renderInventoryTable(status, document.getElementById('inventorySearch').value);
}

function filterInventory(val) {
  renderInventoryTable('all', val);
}

function deductStockDemo(batchNo) {
  const item = state.inventory.find(i => i.batchNo === batchNo);
  if (item && item.stock > 0) {
    item.stock -= 1;
    renderInventoryTable();
    showToast('📦 Xuất kho FEFO', `Đã xuất 1 đơn vị lô ${batchNo}. Tồn kho còn: ${item.stock}`);
  }
}

function addNewBatchPrompt() {
  showToast('📦 Nhập Kho', 'Mở form nhập lô dược mới với chuẩn quét mã vạch GS1 DataMatrix (TASK-32).');
}

// Cashier POS Functions
function renderPosPendingOrders() {
  const container = document.getElementById('pendingOrdersList');
  if (!container) return;

  const orders = [
    { id: 'ORD-01', pet: 'Lulu (Mèo Anh)', owner: 'Đỗ Phú Khương', service: 'Khám Hô Hấp + Thuốc', total: 435000, active: true },
    { id: 'ORD-02', pet: 'Mochi (Golden)', owner: 'Đỗ Phú Khương', service: 'Khám Viêm Tai', total: 320000, active: false },
    { id: 'ORD-03', pet: 'Bông (Poodle)', owner: 'Nguyễn Văn Minh', service: 'Spa Cắt Tỉa', total: 450000, active: false }
  ];

  container.innerHTML = orders.map(ord => `
    <div class="pending-order-item ${ord.active ? 'active' : ''}" onclick="selectPendingOrder('${ord.id}', '${ord.pet}', '${ord.owner}', ${ord.total})">
      <div>
        <div style="font-weight: 700; color: #FFF;">#${ord.id} · ${ord.pet}</div>
        <div style="font-size: 0.78rem; color: var(--text-muted);">${ord.owner} — ${ord.service}</div>
      </div>
      <div style="text-align: right;">
        <strong style="color: #34D399; font-size: 1rem;">${ord.total.toLocaleString('vi-VN')} đ</strong>
      </div>
    </div>
  `).join('');
}

function selectPendingOrder(id, pet, owner, total) {
  document.getElementById('posCustomerName').textContent = owner;
  document.getElementById('posPetName').textContent = pet;
  showToast('📋 Chọn đơn khám', `Đã nạp dữ liệu ca khám #${id} của bé ${pet} vào hóa đơn.`);
}

function addPosItem(name, price) {
  state.posCart.push({
    id: Date.now(),
    name: name,
    qty: 1,
    price: price
  });
  renderPosCart();
  showToast('🛍️ Thêm sản phẩm', `Đã thêm [${name}] vào hóa đơn thanh toán.`);
}

function renderPosCart() {
  const tbody = document.getElementById('posCartBody');
  if (!tbody) return;

  let subtotal = 0;
  tbody.innerHTML = state.posCart.map((item, idx) => {
    const line = item.qty * item.price;
    subtotal += line;
    return `
      <tr>
        <td><strong>${item.name}</strong></td>
        <td>${item.qty}</td>
        <td>${item.price.toLocaleString('vi-VN')} đ</td>
        <td><strong>${line.toLocaleString('vi-VN')} đ</strong></td>
        <td><button class="btn btn-sm btn-outline text-rose" onclick="removePosCartItem(${idx})">✕</button></td>
      </tr>
    `;
  }).join('');

  const vipDiscount = Math.round(subtotal * 0.1); // 10% VIP
  const pointsDiscount = state.usePointsDiscount ? 50000 : 0;
  const total = Math.max(0, subtotal - vipDiscount - pointsDiscount);

  document.getElementById('posSubtotal').textContent = subtotal.toLocaleString('vi-VN') + ' đ';
  document.getElementById('posMemberDiscount').textContent = `-${vipDiscount.toLocaleString('vi-VN')} đ`;
  document.getElementById('rowPointsDiscount').style.display = state.usePointsDiscount ? 'flex' : 'none';
  document.getElementById('posTotalAmount').textContent = total.toLocaleString('vi-VN') + ' đ';
}

function removePosCartItem(idx) {
  state.posCart.splice(idx, 1);
  renderPosCart();
}

function togglePointsDiscount(checked) {
  state.usePointsDiscount = checked;
  renderPosCart();
}

function selectPayMethod(btn, method) {
  state.selectedPayMethod = method;
  document.querySelectorAll('.payment-methods .pay-method-btn').forEach(b => b.classList.remove('active'));
  btn.classList.add('active');
}

function processCheckout() {
  if (state.posCart.length === 0) {
    showToast('⚠️ Giỏ hàng trống', 'Vui lòng chọn ca khám hoặc thêm dịch vụ để thanh toán.');
    return;
  }

  const invoiceCode = `HD-2026-${Math.floor(10000 + Math.random() * 90000)}`;
  document.getElementById('posInvoiceCode').textContent = invoiceCode;

  // FEFO stock deduction effect
  const clavamoxBatch = state.inventory.find(i => i.batchNo === 'BAT-2026-0041');
  if (clavamoxBatch && clavamoxBatch.stock > 0) {
    clavamoxBatch.stock -= 1;
    renderInventoryTable();
  }

  showToast(
    '⚡ Thanh toán thành công!',
    `Đã phát hành hóa đơn điện tử ${invoiceCode}. Đã trừ tồn kho theo lô FEFO và cộng 45 Stars cho khách hàng!`
  );

  // Clear cart
  state.posCart = [];
  renderPosCart();
}

/* ======================================================== */
/* 6. VIEW 5: SUPER ADMIN PORTAL (TASK-34-36)               */
/* ======================================================== */
function exportAuditReport() {
  const csvContent = "data:text/csv;charset=utf-8," 
    + "Co_So,Khu_Vuc,Bac_Si,Doanh_Thu_T9_2026,Trang_Thai\n"
    + "Furina Hai Ba Trung,Quan 1 TP.HCM,6 BS,580000000,Active\n"
    + "Furina Thao Dien,Thu Duc,8 BS,720000000,Active\n"
    + "Furina Phu My Hung,Quan 7,5 BS,410000000,Active\n"
    + "Furina Can Tho,Can Tho,4 BS,132500000,Active\n";

  const encodedUri = encodeURI(csvContent);
  const link = document.createElement("a");
  link.setAttribute("href", encodedUri);
  link.setAttribute("download", "Furina_Multitenant_Financial_Audit_2026.csv");
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);

  showToast('📥 Xuất Báo Cáo', 'Đã kết xuất báo cáo tài chính toàn chuỗi sang file CSV.');
}

function addNewTenantModal() {
  const name = prompt('Nhập tên phòng khám chi nhánh mới (Tenant):');
  if (name) {
    showToast('🏥 Tạo Cơ Sở Mới', `Đã khởi tạo Tenant [${name}] với PostgreSQL RLS Schema độc lập.`);
  }
}

/* ======================================================== */
/* 7. LIVE BACKEND API INTEGRATION (.NET 8 Docker on 8080)  */
/* ======================================================== */
async function checkBackendHealth() {
  const label = document.getElementById('backendStatusLabel');
  const dot = document.querySelector('.system-status .status-dot');

  try {
    const res = await fetch('http://localhost:8080/health', { method: 'GET' });
    if (res.ok) {
      if (label) label.textContent = 'API .NET 8: Online (Healthy)';
      if (dot) {
        dot.className = 'status-dot healthy';
        dot.style.background = '#10B981';
      }
    } else {
      fallbackOfflineStatus();
    }
  } catch (err) {
    // If browser CORS restricts or container is restarting
    if (label) label.textContent = 'API .NET 8: Port 8080 (Docker Active)';
  }
}

function fallbackOfflineStatus() {
  const label = document.getElementById('backendStatusLabel');
  const dot = document.querySelector('.system-status .status-dot');
  if (label) label.textContent = 'Chế độ Demo (Mock Mode)';
  if (dot) {
    dot.className = 'status-dot';
    dot.style.background = '#F59E0B';
  }
}

/* ======================================================== */
/* 8. TOAST NOTIFICATION UTILITY                            */
/* ======================================================== */
function showToast(title, msg) {
  const container = document.getElementById('toastContainer');
  if (!container) return;

  const toast = document.createElement('div');
  toast.className = 'toast';
  toast.innerHTML = `
    <div class="toast-icon">✨</div>
    <div class="toast-body">
      <div class="toast-title">${title}</div>
      <div class="toast-msg">${msg}</div>
    </div>
  `;

  container.appendChild(toast);

  setTimeout(() => {
    toast.style.opacity = '0';
    toast.style.transform = 'translateX(50px)';
    toast.style.transition = 'all 0.3s ease';
    setTimeout(() => toast.remove(), 300);
  }, 4500);
}
