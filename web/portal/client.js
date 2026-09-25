/**
 * FURINA — Client Portal Logic
 * Clean, lightweight, intuitive end-user interactions
 */

const clientPets = {
  mochi: {
    name: 'Mochi',
    avatar: '🐶',
    breed: 'Golden Retriever (Thuần chủng)',
    age: '2 năm 4 tháng',
    weight: '28.5 kg',
    rfid: 'RFID CHIP: 982.014.281.092',
    owner: 'Đỗ Phú Khương (0918.xxx.789)',
    stars: '1,450 Stars',
    vaccines: [
      { name: 'Vanguard Plus 5/L (Mũi 1)', date: '15/01/2025', status: 'done', doctor: 'BS. Vy' },
      { name: 'Vanguard Plus 5/L (Mũi 2)', date: '05/02/2025', status: 'done', doctor: 'BS. Vy' },
      { name: 'Vắc-xin Dại Rabisin (1 Năm)', date: '12/03/2025', status: 'done', doctor: 'BS. Hùng' },
      { name: 'Tiêm Nhắc 7 Bệnh Định Kỳ', date: '12/03/2026', status: 'due', doctor: 'Dự kiến: BS. Vy' }
    ]
  },
  lulu: {
    name: 'Lulu',
    avatar: '🐱',
    breed: 'Mèo Anh Lông Ngắn (Silver Tabby)',
    age: '1 năm 8 tháng',
    weight: '4.2 kg',
    rfid: 'RFID CHIP: 982.014.390.412',
    owner: 'Đỗ Phú Khương (0918.xxx.789)',
    stars: '820 Stars',
    vaccines: [
      { name: 'Nobivac Tricat Trio (4 Bệnh Mũi 1)', date: '10/04/2025', status: 'done', doctor: 'BS. Vy' },
      { name: 'Nobivac Tricat Trio (Mũi 2)', date: '08/05/2025', status: 'done', doctor: 'BS. Vy' },
      { name: 'Vắc-xin Dại Mèo Rabisin', date: '08/06/2025', status: 'done', doctor: 'BS. Hùng' },
      { name: 'Nhắc Lại 4 Bệnh Định Kỳ', date: '08/06/2026', status: 'due', doctor: 'Dự kiến: BS. Hùng' }
    ]
  },
  bong: {
    name: 'Bông',
    avatar: '🐩',
    breed: 'Poodle Teacup Trắng',
    age: '11 tháng',
    weight: '3.8 kg',
    rfid: 'RFID CHIP: 982.014.887.103',
    owner: 'Nguyễn Văn Minh (0933.xxx.567)',
    stars: '560 Stars',
    vaccines: [
      { name: 'Vanguard Plus 5/L (Mũi 1)', date: '10/11/2025', status: 'done', doctor: 'BS. Hùng' },
      { name: 'Vanguard Plus 5/L (Mũi 2)', date: '01/12/2025', status: 'done', doctor: 'BS. Hùng' },
      { name: 'Vắc-xin Dại', date: '20/12/2025', status: 'done', doctor: 'BS. Vy' }
    ]
  }
};

let currentSelectedSlot = '09:30';

document.addEventListener('DOMContentLoaded', () => {
  // Set default booking date to tomorrow
  const tomorrow = new Date();
  tomorrow.setDate(tomorrow.getDate() + 1);
  const dateInput = document.getElementById('clientBookingDate');
  if (dateInput) {
    dateInput.value = tomorrow.toISOString().split('T')[0];
  }

  // Render initial pet passport
  renderPassport('mochi');
});

function selectClientSlot(element, slot) {
  currentSelectedSlot = slot;
  document.querySelectorAll('#clientSlotGrid .slot-chip').forEach(el => el.classList.remove('active'));
  element.classList.add('active');
}

function updateServicePrice() {
  const serviceSelect = document.getElementById('clientService');
  const price = parseInt(serviceSelect.value);
  document.getElementById('clientFeeDisplay').textContent = price.toLocaleString('vi-VN') + ' đ';
}

function showPetPassport(petKey, btn) {
  if (btn) {
    document.querySelectorAll('#passport button').forEach(b => {
      b.style.borderColor = 'var(--border-light)';
      b.style.background = 'transparent';
      b.style.color = 'var(--text-body)';
    });
    btn.style.borderColor = 'var(--teal-primary)';
    btn.style.background = 'var(--teal-light)';
    btn.style.color = 'var(--teal-dark)';
  }
  renderPassport(petKey);
}

function renderPassport(petKey) {
  const pet = clientPets[petKey];
  if (!pet) return;

  document.getElementById('passAvatar').textContent = pet.avatar;
  document.getElementById('passName').textContent = pet.name;
  document.getElementById('passBreed').textContent = pet.breed;
  document.getElementById('passAgeWeight').textContent = `Tuổi: ${pet.age} · Cân nặng: ${pet.weight}`;
  document.getElementById('passRfid').textContent = pet.rfid;
  document.getElementById('passOwner').textContent = pet.owner.split('(')[0].trim();
  document.getElementById('passStars').textContent = pet.stars;

  const vaccineList = document.getElementById('passVaccineList');
  vaccineList.innerHTML = pet.vaccines.map(v => `
    <div class="vaccine-row">
      <div class="v-info">
        <strong>${v.name}</strong>
        <span>Thời gian: ${v.date} · Bác sĩ: ${v.doctor}</span>
      </div>
      <span class="badge ${v.status === 'done' ? 'badge-success' : 'badge-warning'}">
        ${v.status === 'done' ? '✓ Đã tiêm' : '⏳ Cần tiêm nhắc'}
      </span>
    </div>
  `).join('');
}

function handleClientBooking(e) {
  e.preventDefault();

  const petName = document.getElementById('clientPetName').value;
  const petType = document.getElementById('clientPetType').value;
  const serviceSelect = document.getElementById('clientService');
  const serviceName = serviceSelect.options[serviceSelect.selectedIndex].getAttribute('data-name');
  const price = parseInt(serviceSelect.value);
  const clinicSelect = document.getElementById('clientClinic');
  const clinicName = clinicSelect.options[clinicSelect.selectedIndex].text;
  const doctor = document.getElementById('clientDoctor').value;
  const date = document.getElementById('clientBookingDate').value;
  const phone = document.getElementById('clientPhone').value;

  const apptId = `FUR-${Math.floor(1000 + Math.random() * 9000)}`;

  // Store in localStorage for admin synchronization
  const newAppointment = {
    id: apptId,
    time: currentSelectedSlot,
    date: date,
    petName: petName,
    petType: petType,
    breed: petType === 'cat' ? 'Mèo cưng' : 'Chó cưng',
    avatar: petType === 'cat' ? '🐱' : '🐶',
    owner: `${phone}`,
    service: serviceName,
    doctor: doctor,
    clinic: clinicName,
    room: 'Phòng Tiếp Nhận Chờ',
    status: 'scheduled',
    weight: petType === 'cat' ? 4.0 : 12.0,
    cost: price
  };

  const stored = JSON.parse(localStorage.getItem('furina_custom_appts') || '[]');
  stored.unshift(newAppointment);
  localStorage.setItem('furina_custom_appts', JSON.stringify(stored));

  // Open modern modal
  showBookingSuccessModal(newAppointment);
  showClientToast('🎉 Đặt lịch thành công', `Mã phiếu hẹn #${apptId} đã gửi tới Bàn điều phối.`, '📅');
}

function showBookingSuccessModal(appt) {
  const modal = document.getElementById('bookingModal');
  if (!modal) return;

  document.getElementById('modalApptCode').textContent = `#${appt.id}`;
  document.getElementById('modalPetInfo').textContent = `${appt.petName} (${appt.breed})`;
  document.getElementById('modalDateTime').textContent = `${appt.time} · Ngày ${appt.date}`;
  document.getElementById('modalDoctor').textContent = appt.doctor;
  document.getElementById('modalService').textContent = appt.service;
  document.getElementById('modalClinic').textContent = appt.clinic;
  document.getElementById('modalPrice').textContent = appt.cost.toLocaleString('vi-VN') + ' đ';

  modal.classList.add('active');
}

function closeBookingModal() {
  const modal = document.getElementById('bookingModal');
  if (modal) modal.classList.remove('active');
}

function setPetType(type) {
  document.querySelectorAll('.pet-type-btn').forEach(btn => {
    btn.classList.toggle('active', btn.getAttribute('data-type') === type);
  });
  const typeSelect = document.getElementById('clientPetType');
  if (typeSelect) {
    typeSelect.value = type;
    updateServicePrice();
  }
}

function showClientToast(title, msg, icon = '✨') {
  let shelf = document.getElementById('clientToastShelf');
  if (!shelf) {
    shelf = document.createElement('div');
    shelf.id = 'clientToastShelf';
    shelf.className = 'toast-shelf';
    document.body.appendChild(shelf);
  }

  const toast = document.createElement('div');
  toast.className = 'toast-msg';
  toast.innerHTML = `
    <div class="t-icon">${icon}</div>
    <div class="t-body">
      <h5>${title}</h5>
      <p>${msg}</p>
    </div>
  `;

  shelf.appendChild(toast);

  setTimeout(() => {
    toast.style.opacity = '0';
    toast.style.transform = 'translateY(15px)';
    toast.style.transition = 'all 0.25s ease';
    setTimeout(() => toast.remove(), 250);
  }, 4000);
}
