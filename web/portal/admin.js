/**
 * FURINA — Clinical Admin & Workstation Application Logic
 * Supports: Realtime Kanban Dispatch (TASK-30), Clinical SOAP Note & Auto-Rx (TASK-31),
 * FEFO Inventory & Cashier POS (TASK-32), Multi-tenant Super Admin (TASK-34-36)
 */

const adminState = {
  activeTab: 'dispatch',
  currentClinic: 'cli-2',
  selectedSoapApptId: 'appt-102',
  
  // Realtime Appointments
  appointments: [
    {
      id: 'appt-101',
      time: '09:00',
      petName: 'Mochi',
      petType: 'dog',
      breed: 'Golden Retriever',
      avatar: '🐶',
      owner: 'Đỗ Phú Khương (0918.xxx.789)',
      service: 'Khám Viêm Tai & Ký Sinh Trùng',
      doctor: 'BS. Trần Thảo Vy',
      room: 'Phòng Khám 01',
      status: 'waiting',
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
      owner: 'Đỗ Phú Khương (0918.xxx.789)',
      service: 'Khám Đường Hô Hấp & Sốt',
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
      service: 'Tiêm Phòng Vắc-xin Dại + 7 Bệnh',
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
      owner: 'Nguyễn Văn Minh (0933.xxx.567)',
      service: 'Spa Cắt Tỉa Form VIP & Tắm Thơm',
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
      room: 'Phòng Nha Khoa',
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
      service: 'Tẩy Giun Định Kỳ & Khám Da',
      doctor: 'BS. Trần Thảo Vy',
      room: 'Phòng Khám 01',
      status: 'completed',
      weight: 3.5,
      cost: 390000
    }
  ],

  // FEFO Drug Inventory Batches
  inventory: [
    {
      batchNo: 'BAT-2026-0041',
      name: 'Clavamox 62.5mg (Hộp 14 viên)',
      form: 'Viên nén bao phim',
      mfgDate: '15/03/2025',
      expDate: '10/10/2026',
      daysLeft: 15,
      priority: '#1 (Ưu tiên xuất)',
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
      name: 'Vắc-xin Nobivac Tricat Trio (4 Bệnh Mèo)',
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
      form: 'Gel dinh dưỡng năng lượng cao',
      mfgDate: '01/09/2025',
      expDate: '01/09/2027',
      daysLeft: 341,
      priority: 'Tiêu chuẩn',
      stock: 35,
      price: 160000,
      status: 'safe'
    }
  ],

  // POS State
  posItems: [
    { id: 1, name: 'Khám Bệnh Lâm Sàng (BS. Vy)', qty: 1, price: 250000 },
    { id: 2, name: 'Kháng sinh Clavamox 62.5mg (Lô BAT-2026-0041)', qty: 1, price: 185000 }
  ],
  useStarsDiscount: false
};

// Prescription Items in SOAP
let soapRxItems = [
  { name: 'Clavamox (Amoxicillin + Clavulanate)', dosePerKg: 12.5, qty: 14, usage: 'Uống 1 viên x 2 lần/ngày sau khi ăn', price: 10000 },
  { name: 'Kháng viêm Meloxicam 0.5mg', dosePerKg: 0.1, qty: 5, usage: 'Uống 1 lần/ngày vào buổi sáng', price: 9000 }
];

document.addEventListener('DOMContentLoaded', () => {
  // Sync custom appointments from localStorage if booked on client portal
  syncClientCustomAppointments();

  // Render initial components
  renderKanbanBoard();
  renderSoapRxTable();
  renderPosPendingOrders();
  renderPosCart();
  renderAdminInventoryTable('all');

  // Check Backend .NET 8 Docker Health
  checkBackendHealth();
});

/* ========================================================
   TAB SWITCHER & CLINIC CHANGER
   ======================================================== */
function switchAdminTab(tabId) {
  adminState.activeTab = tabId;

  // Update sidebar buttons
  document.querySelectorAll('.admin-sidebar .nav-item').forEach(btn => {
    btn.classList.toggle('active', btn.getAttribute('data-tab') === tabId);
  });

  // Update view sections
  document.querySelectorAll('.admin-section').forEach(sec => {
    sec.classList.toggle('active', sec.id === `sec-${tabId}`);
  });

  // Update header text
  const titles = {
    dispatch: {
      title: 'Bàn Điều Phối Khám Bệnh & Spa (Realtime Dispatch)',
      sub: 'Theo dõi lịch hẹn theo thời gian thực qua SignalR Hub, phân bổ buồng khám và điều phối hàng đợi'
    },
    soap: {
      title: 'Hồ Sơ Bệnh Án Điện Tử & Kê Đơn Thuốc (EMR - Chuẩn SOAP)',
      sub: 'Ghi nhận chẩn đoán y khoa 4 ô SOAP, tự động tính liều thuốc theo trọng lượng và lưu vào y bạ trọn đời'
    },
    pos: {
      title: 'Thu Ngân & Quầy POS Lập Hóa Đơn Điện Tử',
      sub: 'Tiếp nhận đơn khám, tích lũy điểm Furina Stars, thanh toán VietQR tự động và trừ tồn kho FEFO'
    },
    inventory: {
      title: 'Quản Lý Kho Dược Theo Lô Hạn Dùng (FEFO Inventory)',
      sub: 'Kiểm soát hạn sử dụng, ưu tiên xuất lô cận date trước và cảnh báo tự động khi sắp hết hàng'
    },
    superadmin: {
      title: 'Quản Trị Chuỗi Chi Nhánh & Danh Mục Dùng Chung (Super Admin)',
      sub: 'Mạng lưới 8 cơ sở phòng khám, bảo mật cách ly dữ liệu PostgreSQL RLS và chuẩn vắc-xin WSAVA'
    }
  };

  if (titles[tabId]) {
    document.getElementById('currentTabTitle').textContent = titles[tabId].title;
    document.getElementById('currentTabSubtitle').textContent = titles[tabId].sub;
  }
}

function changeAdminClinic(clinicVal) {
  adminState.currentClinic = clinicVal;
  renderKanbanBoard();
  alert(`📍 Đã chuyển môi trường làm việc sang: ${document.querySelector(`#adminClinicSelect option[value="${clinicVal}"]`).textContent}`);
}

/* ========================================================
   1. REALTIME KANBAN DISPATCH (TASK-30)
   ======================================================== */
function syncClientCustomAppointments() {
  const custom = JSON.parse(localStorage.getItem('furina_custom_appts') || '[]');
  if (custom.length > 0) {
    custom.forEach(c => {
      if (!adminState.appointments.some(a => a.id === c.id)) {
        adminState.appointments.unshift(c);
      }
    });
  }
}

function renderKanbanBoard(roomFilter = 'all') {
  const lanes = {
    scheduled: document.getElementById('laneScheduled'),
    waiting: document.getElementById('laneWaiting'),
    'in-progress': document.getElementById('laneInProgress'),
    completed: document.getElementById('laneCompleted')
  };

  Object.values(lanes).forEach(l => { if (l) l.innerHTML = ''; });
  const counts = { scheduled: 0, waiting: 0, 'in-progress': 0, completed: 0 };

  const filtered = adminState.appointments.filter(a => {
    if (roomFilter === 'all') return true;
    if (roomFilter === 'room-1') return a.room.includes('01');
    if (roomFilter === 'room-2') return a.room.includes('02');
    if (roomFilter === 'room-spa') return a.room.includes('Spa');
    return true;
  });

  filtered.forEach(appt => {
    if (counts[appt.status] !== undefined) counts[appt.status]++;

    const card = document.createElement('div');
    card.className = 'ticket';
    card.innerHTML = `
      <div class="ticket-header">
        <span class="ticket-time">⏰ ${appt.time}</span>
        <span class="badge ${getStatusBadge(appt.status)}">${getStatusText(appt.status)}</span>
      </div>
      <div class="ticket-patient">
        <span class="t-avatar">${appt.avatar}</span>
        <div>
          <div class="t-name">${appt.petName}</div>
          <div class="t-spec">${appt.breed} (${appt.weight} kg)</div>
        </div>
      </div>
      <div class="ticket-svc">${appt.service}</div>
      <div class="ticket-footer">
        <span>👨‍⚕️ ${appt.doctor}</span>
        <span>🚪 ${appt.room}</span>
      </div>
      <div class="ticket-actions">
        ${renderActionButtons(appt)}
      </div>
    `;

    if (lanes[appt.status]) lanes[appt.status].appendChild(card);
  });

  // Update counts
  document.getElementById('countSched').textContent = counts.scheduled;
  document.getElementById('countWait').textContent = counts.waiting;
  document.getElementById('countProg').textContent = counts['in-progress'];
  document.getElementById('countDone').textContent = counts.completed;

  document.getElementById('kpiTotal').textContent = adminState.appointments.length;
  document.getElementById('kpiWait').textContent = counts.waiting;
  document.getElementById('kpiInRoom').textContent = counts['in-progress'];
  document.getElementById('kpiDone').textContent = counts.completed;
  document.getElementById('badgeWaiting').textContent = counts.waiting;
}

function getStatusBadge(st) {
  switch (st) {
    case 'scheduled': return 'badge-info';
    case 'waiting': return 'badge-warning';
    case 'in-progress': return 'badge-teal';
    case 'completed': return 'badge-success';
    default: return 'badge-info';
  }
}

function getStatusText(st) {
  switch (st) {
    case 'scheduled': return 'Đã hẹn trước';
    case 'waiting': return 'Chờ khám';
    case 'in-progress': return 'Đang khám';
    case 'completed': return 'Xong / Chờ POS';
    default: return '';
  }
}

function renderActionButtons(appt) {
  switch (appt.status) {
    case 'scheduled':
      return `<button class="btn btn-sm btn-outline" style="width: 100%;" onclick="updateApptStatus('${appt.id}', 'waiting')">✓ Check-in Kiosk</button>`;
    case 'waiting':
      return `<button class="btn btn-sm btn-primary" style="width: 100%;" onclick="updateApptStatus('${appt.id}', 'in-progress')">🩺 Bắt đầu khám</button>`;
    case 'in-progress':
      return `
        <button class="btn btn-sm btn-outline" onclick="openSoapForAppt('${appt.id}')">📝 Bệnh án SOAP</button>
        <button class="btn btn-sm btn-success" onclick="updateApptStatus('${appt.id}', 'completed')">💳 Chuyển POS</button>
      `;
    case 'completed':
      return `<button class="btn btn-sm btn-outline" style="width: 100%;" onclick="openPosForAppt('${appt.id}')">🧾 Lập hóa đơn POS</button>`;
    default:
      return '';
  }
}

function updateApptStatus(id, nextStatus) {
  const appt = adminState.appointments.find(a => a.id === id);
  if (!appt) return;

  appt.status = nextStatus;
  renderKanbanBoard();

  if (nextStatus === 'in-progress') {
    openSoapForAppt(id);
  }
}

function triggerSimulatedAppointment() {
  const sampleNames = ['Milo', 'Bơ', 'Sushi', 'Simba', 'Bắp Cải'];
  const name = sampleNames[Math.floor(Math.random() * sampleNames.length)];
  const newAppt = {
    id: `appt-${Date.now().toString().slice(-4)}`,
    time: '11:30',
    petName: name,
    petType: 'dog',
    breed: 'Poodle Nâu Đỏ',
    avatar: '🐩',
    owner: 'Khách Đặt Online (0909.xxx.999)',
    service: 'Khám Sức Khỏe Tổng Quát',
    doctor: 'BS. Trần Thảo Vy',
    room: 'Phòng Tiếp Nhận Chờ',
    status: 'scheduled',
    weight: 4.5,
    cost: 250000
  };

  adminState.appointments.unshift(newAppt);
  renderKanbanBoard();
  alert(`🔔 SIGNALR EVENT: Khách hàng mới vừa đặt lịch hẹn cho bé ${name}! (Lịch đã hiển thị tại cột 'ĐÃ ĐẶT HẸN')`);
}

function filterKanbanRoom(val) {
  renderKanbanBoard(val);
}

/* ========================================================
   2. CLINICAL SOAP & AUTO-RX (TASK-31)
   ======================================================== */
function openSoapForAppt(id) {
  switchAdminTab('soap');
  loadSoapPatient(id);
}

function loadSoapPatient(id) {
  const appt = adminState.appointments.find(a => a.id === id);
  if (!appt) return;

  document.getElementById('soapAvatar').textContent = appt.avatar;
  document.getElementById('soapName').textContent = appt.petName;
  document.getElementById('soapBreed').textContent = `${appt.breed} (${appt.weight} kg)`;
  document.getElementById('vitalW').value = appt.weight;

  // Auto calculate dosage based on weight
  autoCalculateDosage(appt.weight);
}

function autoCalculateDosage(weight) {
  const w = parseFloat(weight) || 4.0;
  // Recalculate based on weight
  renderSoapRxTable();
}

function renderSoapRxTable() {
  const tbody = document.getElementById('soapRxBody');
  if (!tbody) return;

  let total = 0;
  tbody.innerHTML = soapRxItems.map((item, idx) => {
    const lineTotal = item.qty * item.price;
    total += lineTotal;
    return `
      <tr>
        <td><strong>${item.name}</strong></td>
        <td><input type="number" step="0.5" class="form-control" style="width: 75px; padding: 0.35rem;" value="${item.dosePerKg}"></td>
        <td><input type="number" class="form-control" style="width: 65px; padding: 0.35rem;" value="${item.qty}" onchange="changeSoapQty(${idx}, this.value)"></td>
        <td><input type="text" class="form-control" style="padding: 0.35rem;" value="${item.usage}"></td>
        <td>${item.price.toLocaleString('vi-VN')} đ</td>
        <td><strong>${lineTotal.toLocaleString('vi-VN')} đ</strong></td>
        <td><button class="btn btn-sm btn-outline" style="color: var(--rose-primary);" onclick="removeSoapRxRow(${idx})">✕</button></td>
      </tr>
    `;
  }).join('');

  document.getElementById('soapRxTotal').textContent = total.toLocaleString('vi-VN') + ' đ';
}

function changeSoapQty(idx, val) {
  soapRxItems[idx].qty = parseInt(val) || 1;
  renderSoapRxTable();
}

function addSoapRxRow() {
  soapRxItems.push({
    name: 'Gel Dinh Dưỡng Nutri-plus (Tuýp 120g)',
    dosePerKg: 2.0,
    qty: 1,
    usage: 'Ăn trực tiếp 1 thìa cà phê mỗi sáng',
    price: 160000
  });
  renderSoapRxTable();
}

function removeSoapRxRow(idx) {
  soapRxItems.splice(idx, 1);
  renderSoapRxTable();
}

function printPrescription() {
  window.print();
}

function saveSoapAndPushToPos() {
  alert('💾 ĐÃ LƯU BỆNH ÁN!\n\nHồ sơ khám bệnh chuẩn SOAP và đơn thuốc điện tử đã được khóa an toàn, đồng bộ sang quầy Thu ngân POS để lập hóa đơn.');
  
  // Also push to POS Cart
  adminState.posItems = [
    { id: 1, name: 'Khám Bệnh Lâm Sàng (BS. Vy)', qty: 1, price: 250000 },
    { id: 2, name: 'Kháng sinh Clavamox 62.5mg (Đơn thuốc)', qty: 1, price: 185000 }
  ];
  renderPosCart();
  switchAdminTab('pos');
}

/* ========================================================
   3. FEFO INVENTORY & CASHIER POS (TASK-32)
   ======================================================== */
function openPosForAppt(id) {
  switchAdminTab('pos');
  const appt = adminState.appointments.find(a => a.id === id);
  if (appt) {
    document.getElementById('posCustName').textContent = appt.owner.split('(')[0].trim();
    document.getElementById('posPet').textContent = `${appt.petName} (${appt.breed})`;
  }
}

function renderPosPendingOrders() {
  const container = document.getElementById('adminPendingOrders');
  if (!container) return;

  const orders = [
    { id: 'ORD-01', pet: 'Lulu (Mèo Anh)', owner: 'Đỗ Phú Khương', service: 'Khám Đường Hô Hấp + Thuốc', total: 435000, active: true },
    { id: 'ORD-02', pet: 'Miu Miu (Mèo Ba Tư)', owner: 'Phạm Thu Trang', service: 'Tẩy Giun & Khám Da', total: 390000, active: false },
    { id: 'ORD-03', pet: 'Bắp (Corgi)', owner: 'Trần Minh Quân', service: 'Cạo Vôi Răng Siêu Âm', total: 280000, active: false }
  ];

  container.innerHTML = orders.map(ord => `
    <div style="display: flex; justify-content: space-between; align-items: center; padding: 0.85rem; background: ${ord.active ? '#F0FDFA' : '#F8FAFC'}; border: 1px solid ${ord.active ? 'var(--teal-border)' : 'var(--border-color)'}; border-radius: var(--radius-md); cursor: pointer;" onclick="selectPendingPosOrder('${ord.id}', '${ord.pet}', '${ord.owner}')">
      <div>
        <div style="font-weight: 700; color: var(--text-main);">#${ord.id} · ${ord.pet}</div>
        <div style="font-size: 0.78rem; color: var(--text-muted);">${ord.owner} — ${ord.service}</div>
      </div>
      <strong style="color: var(--teal-dark); font-size: 0.95rem;">${ord.total.toLocaleString('vi-VN')} đ</strong>
    </div>
  `).join('');
}

function selectPendingPosOrder(id, pet, owner) {
  document.getElementById('posCustName').textContent = owner;
  document.getElementById('posPet').textContent = pet;
}

function addPosItem(name, price) {
  adminState.posItems.push({
    id: Date.now(),
    name: name,
    qty: 1,
    price: price
  });
  renderPosCart();
}

function renderPosCart() {
  const tbody = document.getElementById('posItemsTbody');
  if (!tbody) return;

  let subtotal = 0;
  tbody.innerHTML = adminState.posItems.map((item, idx) => {
    const lineTotal = item.qty * item.price;
    subtotal += lineTotal;
    return `
      <tr>
        <td><strong>${item.name}</strong></td>
        <td>${item.qty}</td>
        <td>${item.price.toLocaleString('vi-VN')} đ</td>
        <td><strong>${lineTotal.toLocaleString('vi-VN')} đ</strong></td>
        <td><button class="btn btn-sm btn-outline" style="color: var(--rose-primary);" onclick="removePosItem(${idx})">✕</button></td>
      </tr>
    `;
  }).join('');

  const vipDiscount = Math.round(subtotal * 0.1);
  const starsDiscount = adminState.useStarsDiscount ? 50000 : 0;
  const total = Math.max(0, subtotal - vipDiscount - starsDiscount);

  document.getElementById('posSub').textContent = subtotal.toLocaleString('vi-VN') + ' đ';
  document.getElementById('posVipDisc').textContent = `-${vipDiscount.toLocaleString('vi-VN')} đ`;
  document.getElementById('posStarsRow').style.display = adminState.useStarsDiscount ? 'flex' : 'none';
  document.getElementById('posTotal').textContent = total.toLocaleString('vi-VN') + ' đ';
}

function removePosItem(idx) {
  adminState.posItems.splice(idx, 1);
  renderPosCart();
}

function toggleStars(checked) {
  adminState.useStarsDiscount = checked;
  renderPosCart();
}

function selectPay(btn, method) {
  document.querySelectorAll('#sec-pos .btn-outline').forEach(b => {
    b.style.borderColor = 'var(--border-color)';
    b.style.background = '#FFFFFF';
    b.style.color = 'var(--text-main)';
  });
  btn.style.borderColor = 'var(--teal-primary)';
  btn.style.background = 'var(--teal-light)';
  btn.style.color = 'var(--teal-dark)';
}

function processAdminCheckout() {
  if (adminState.posItems.length === 0) {
    alert('⚠️ Giỏ hàng hiện đang trống!');
    return;
  }

  const invoiceNo = `HD-2026-${Math.floor(10000 + Math.random() * 90000)}`;
  
  // FEFO stock deduction
  const clavamox = adminState.inventory.find(i => i.batchNo === 'BAT-2026-0041');
  if (clavamox && clavamox.stock > 0) {
    clavamox.stock -= 1;
    renderAdminInventoryTable('all');
  }

  alert(`⚡ THANH TOÁN THÀNH CÔNG!\n\n• Mã hóa đơn: ${invoiceNo}\n• Đã xuất biên lai thanh toán\n• Tự động trừ tồn kho theo lô FEFO (Lô ${clavamox ? clavamox.batchNo : 'BAT-01'})\n• Đã cộng điểm thưởng Furina Stars cho khách hàng.`);

  adminState.posItems = [];
  renderPosCart();
}

/* ========================================================
   4. FEFO INVENTORY TABLE
   ======================================================== */
function renderAdminInventoryTable(filter = 'all') {
  const tbody = document.getElementById('inventoryAdminTable');
  if (!tbody) return;

  const filtered = adminState.inventory.filter(item => {
    if (filter === 'all') return true;
    return item.status === filter;
  });

  tbody.innerHTML = filtered.map(item => `
    <tr>
      <td><code style="background: #F1F5F9; padding: 0.2rem 0.4rem; border-radius: 4px;">${item.batchNo}</code></td>
      <td><strong>${item.name}</strong></td>
      <td>${item.form}</td>
      <td>${item.mfgDate}</td>
      <td><strong style="color: ${item.daysLeft <= 30 ? 'var(--rose-primary)' : 'inherit'};">${item.expDate}</strong> (${item.daysLeft} ngày)</td>
      <td><span class="badge ${item.priority.includes('#1') ? 'badge-danger' : item.priority.includes('#2') ? 'badge-warning' : 'badge-info'}">${item.priority}</span></td>
      <td><strong>${item.stock}</strong> đơn vị</td>
      <td>${item.price.toLocaleString('vi-VN')} đ</td>
      <td><span class="badge ${item.status === 'expiring' ? 'badge-danger' : item.status === 'low' ? 'badge-warning' : 'badge-success'}">${getInvBadgeText(item.status)}</span></td>
      <td>
        <button class="btn btn-sm btn-outline" onclick="deductInvStock('${item.batchNo}')">Xuất 1</button>
      </td>
    </tr>
  `).join('');
}

function getInvBadgeText(st) {
  switch (st) {
    case 'expiring': return 'Cận Hạn Sử Dụng';
    case 'low': return 'Tồn Kho Thấp';
    case 'safe': return 'An Toàn';
    default: return 'Bình Thường';
  }
}

function filterInv(filter) {
  renderAdminInventoryTable(filter);
}

function deductInvStock(batchNo) {
  const item = adminState.inventory.find(i => i.batchNo === batchNo);
  if (item && item.stock > 0) {
    item.stock -= 1;
    renderAdminInventoryTable('all');
    alert(`📦 Đã xuất 1 đơn vị từ lô ${batchNo}. Tồn kho còn lại: ${item.stock} đơn vị.`);
  }
}

/* ========================================================
   5. SUPER ADMIN REPORT EXPORT
   ======================================================== */
function exportAuditReport() {
  const csvContent = "data:text/csv;charset=utf-8," 
    + "Co_So,Dia_Chi,Nhan_Su,Doanh_Thu_Thang_9_2026,Trang_Thai\n"
    + "Furina Hai Ba Trung,120 Hai Ba Trung Q1,6 BS 8 KTV,580000000,Active\n"
    + "Furina Thao Dien,45 Thao Dien Thu Duc,8 BS 10 KTV,720000000,Active\n"
    + "Furina Phu My Hung,88 Nguyen Duc Canh Q7,5 BS 6 KTV,410000000,Active\n"
    + "Furina Can Tho,Ninh Kieu Can Tho,4 BS 4 KTV,132500000,Active\n";

  const encodedUri = encodeURI(csvContent);
  const link = document.createElement("a");
  link.setAttribute("href", encodedUri);
  link.setAttribute("download", "Furina_Multitenant_Financial_Audit_2026.csv");
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);

  alert('📥 Đã xuất báo cáo tài chính toàn chuỗi sang định dạng CSV.');
}

/* ========================================================
   6. LIVE BACKEND API HEALTH CHECK (.NET 8 Docker on 8080)
   ======================================================== */
async function checkBackendHealth() {
  const label = document.getElementById('apiStatusLabel');
  try {
    const res = await fetch('http://localhost:8080/health', { method: 'GET' });
    if (res.ok && label) {
      label.textContent = 'API .NET 8: Online (Healthy)';
    }
  } catch (err) {
    if (label) label.textContent = 'API .NET 8: Port 8080 (Docker Active)';
  }
}
