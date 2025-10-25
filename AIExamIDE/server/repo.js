const { db, migrate } = require('./db');

migrate();

function parseJson(text, fallback = null) {
  if (!text) return fallback;
  try {
    return JSON.parse(text);
  } catch {
    return fallback;
  }
}

function mapRoom(row) {
  if (!row) return null;
  return {
    id: row.id,
    name: row.name,
    created_at: row.created_at,
    updated_at: row.updated_at,
    seatmap: parseJson(row.seatmap_json, { desks: [] })
  };
}

function mapSession(row) {
  if (!row) return null;
  return {
    id: row.id,
    teacher_id: row.teacher_id,
    room_id: row.room_id,
    title: row.title,
    date: row.date,
    start_time: row.start_time,
    end_time: row.end_time,
    exam_type: row.exam_type,
    ai_generated: row.ai_generated,
    status: row.status,
    created_at: row.created_at,
    updated_at: row.updated_at
  };
}

function mapSubmission(row) {
  if (!row) return null;
  return {
    id: row.id,
    booking_id: row.booking_id,
    files_json: row.files_json,
    tasks_json: row.tasks_json,
    csvs_json: row.csvs_json,
    evaluation_json: row.evaluation_json,
    grade_final: row.grade_final,
    feedback_json: row.feedback_json,
    created_at: row.created_at,
    updated_at: row.updated_at
  };
}

/** Users **/
function createUser(email, name, password_hash, role) {
  const stmt = db.prepare('INSERT INTO users (email, name, password_hash, role) VALUES (?, ?, ?, ?)');
  const info = stmt.run(email, name, password_hash, role);
  return { id: info.lastInsertRowid, email, name, role };
}

function getUserByEmail(email) {
  return db.prepare('SELECT * FROM users WHERE lower(email) = lower(?)').get(email) || null;
}

function getUserById(id) {
  return db.prepare('SELECT * FROM users WHERE id = ?').get(Number(id)) || null;
}

/** Rooms **/
function createRoom(name, seatmap) {
  const stmt = db.prepare('INSERT INTO exam_rooms (name, seatmap_json) VALUES (?, ?)');
  const info = stmt.run(name, JSON.stringify(seatmap || { desks: [] }));
  return getRoom(info.lastInsertRowid);
}

function listRooms() {
  return db.prepare('SELECT * FROM exam_rooms ORDER BY id DESC').all().map(mapRoom);
}

function getRoom(id) {
  return mapRoom(db.prepare('SELECT * FROM exam_rooms WHERE id = ?').get(Number(id)));
}

function updateRoom(id, changes) {
  const sets = [];
  const params = [];
  if (changes.name !== undefined && changes.name !== null) {
    sets.push('name = ?');
    params.push(changes.name);
  }
  if (changes.seatmap !== undefined && changes.seatmap !== null) {
    sets.push('seatmap_json = ?');
    params.push(JSON.stringify(changes.seatmap));
  }
  if (sets.length === 0) return getRoom(id);
  params.push(Number(id));
  const sql = `UPDATE exam_rooms SET ${sets.join(', ')} WHERE id = ?`;
  db.prepare(sql).run(...params);
  return getRoom(id);
}

function deleteRoom(id) {
  return db.prepare('DELETE FROM exam_rooms WHERE id = ?').run(Number(id)).changes;
}

/** Sessions **/
function createSession({ teacherId, roomId, title, date, startTime, endTime, examType, aiGenerated }) {
  const stmt = db.prepare('INSERT INTO exam_sessions (teacher_id, room_id, title, date, start_time, end_time, exam_type, ai_generated) VALUES (?, ?, ?, ?, ?, ?, ?, ?)');
  const info = stmt.run(Number(teacherId), Number(roomId), title || null, date, startTime || null, endTime || null, examType || 'java', aiGenerated ? 1 : 0);
  return getSession(info.lastInsertRowid);
}

function listSessionsByTeacher(teacherId) {
  const rows = db.prepare('SELECT * FROM exam_sessions WHERE teacher_id = ? ORDER BY date DESC, start_time DESC').all(Number(teacherId));
  return rows.map(mapSession);
}

function getSessionByIdForTeacher(id, teacherId) {
  const row = db.prepare('SELECT * FROM exam_sessions WHERE id = ? AND teacher_id = ?').get(Number(id), Number(teacherId));
  return mapSession(row);
}

function getSession(id) {
  return mapSession(db.prepare('SELECT * FROM exam_sessions WHERE id = ?').get(Number(id)));
}

function updateSession(id, changes) {
  const sets = [];
  const params = [];
  if (changes.title !== undefined) { sets.push('title = ?'); params.push(changes.title); }
  if (changes.date !== undefined) { sets.push('date = ?'); params.push(changes.date); }
  if (changes.start_time !== undefined) { sets.push('start_time = ?'); params.push(changes.start_time); }
  if (changes.end_time !== undefined) { sets.push('end_time = ?'); params.push(changes.end_time); }
  if (changes.exam_type !== undefined) { sets.push('exam_type = ?'); params.push(changes.exam_type); }
  if (changes.status !== undefined) { sets.push('status = ?'); params.push(changes.status); }
  if (changes.ai_generated !== undefined) { sets.push('ai_generated = ?'); params.push(changes.ai_generated ? 1 : 0); }
  if (sets.length === 0) return getSession(id);
  params.push(Number(id));
  const sql = `UPDATE exam_sessions SET ${sets.join(', ')} WHERE id = ?`;
  db.prepare(sql).run(...params);
  return getSession(id);
}

function listScheduledSessionsFromDate(dateStr) {
  const rows = db.prepare('SELECT * FROM exam_sessions WHERE status = "scheduled" AND date >= ? ORDER BY date, start_time').all(dateStr);
  return rows.map(mapSession);
}

/** Bookings **/
function listBookedSeats(sessionId) {
  return db.prepare('SELECT seat_id FROM bookings WHERE session_id = ?').all(Number(sessionId)).map(r => r.seat_id);
}

function findExistingBooking(sessionId, seatId, studentId) {
  return db.prepare('SELECT * FROM bookings WHERE session_id = ? AND (seat_id = ? OR student_id = ?)').get(Number(sessionId), seatId, Number(studentId)) || null;
}

function createBooking(sessionId, studentId, seatId) {
  const stmt = db.prepare('INSERT INTO bookings (session_id, student_id, seat_id) VALUES (?, ?, ?)');
  const info = stmt.run(Number(sessionId), Number(studentId), seatId);
  return getBookingById(info.lastInsertRowid);
}

function getBookingById(id) {
  return db.prepare('SELECT * FROM bookings WHERE id = ?').get(Number(id)) || null;
}

function listBookingsByStudent(studentId) {
  const sql = `
    SELECT b.*, s.date, s.title, s.start_time, s.end_time, s.room_id, s.exam_type, s.ai_generated, r.name AS room_name
    FROM bookings b
    JOIN exam_sessions s ON s.id = b.session_id
    JOIN exam_rooms r ON r.id = s.room_id
    WHERE b.student_id = ?
    ORDER BY s.date DESC, s.start_time DESC`;
  return db.prepare(sql).all(Number(studentId));
}

/** Submissions **/
function createSubmission({ bookingId, files, tasks, csvs }) {
  const stmt = db.prepare('INSERT INTO submissions (booking_id, files_json, tasks_json, csvs_json) VALUES (?, ?, ?, ?)');
  const info = stmt.run(bookingId ? Number(bookingId) : null,
    files ? JSON.stringify(files) : null,
    tasks ? JSON.stringify(tasks) : null,
    csvs ? JSON.stringify(csvs) : null);
  return mapSubmission(db.prepare('SELECT * FROM submissions WHERE id = ?').get(info.lastInsertRowid));
}

function getSubmission(id) {
  return mapSubmission(db.prepare('SELECT * FROM submissions WHERE id = ?').get(Number(id)));
}

function updateSubmissionFields(id, { grade_final, feedback_json, evaluation_json }) {
  const sets = [];
  const params = [];
  if (grade_final !== undefined) { sets.push('grade_final = ?'); params.push(grade_final); }
  if (feedback_json !== undefined) { sets.push('feedback_json = ?'); params.push(feedback_json); }
  if (evaluation_json !== undefined) { sets.push('evaluation_json = ?'); params.push(evaluation_json); }
  if (sets.length === 0) return getSubmission(id);
  sets.push('updated_at = datetime("now")');
  params.push(Number(id));
  const sql = `UPDATE submissions SET ${sets.join(', ')} WHERE id = ?`;
  db.prepare(sql).run(...params);
  return getSubmission(id);
}

function getLastSubmissionByBooking(bookingId) {
  return mapSubmission(db.prepare('SELECT * FROM submissions WHERE booking_id = ? ORDER BY created_at DESC LIMIT 1').get(Number(bookingId)));
}

function listSubmissions() {
  return db.prepare('SELECT * FROM submissions ORDER BY created_at DESC').all().map(mapSubmission);
}

/** Fallback exam **/
function getFallbackExam() {
  const row = db.prepare('SELECT json FROM fallback_exam WHERE id = 1').get();
  return row ? parseJson(row.json, {}) : null;
}

function setFallbackExam(json) {
  db.prepare('INSERT INTO fallback_exam (id, json) VALUES (1, ?) ON CONFLICT(id) DO UPDATE SET json = excluded.json')
    .run(JSON.stringify(json || {}));
  return true;
}

/** Classes **/
function createClass(teacherId, name) {
  const info = db.prepare('INSERT INTO classes (name, teacher_id) VALUES (?, ?)').run(name, Number(teacherId));
  return db.prepare('SELECT * FROM classes WHERE id = ?').get(info.lastInsertRowid);
}

function listClasses(teacherId) {
  return db.prepare('SELECT * FROM classes WHERE teacher_id = ? ORDER BY created_at DESC').all(Number(teacherId));
}

function countClassStudents(classId) {
  return db.prepare('SELECT COUNT(*) AS n FROM class_students WHERE class_id = ?').get(Number(classId)).n;
}

function addClassStudent(classId, studentId) {
  db.prepare('INSERT OR IGNORE INTO class_students (class_id, student_id) VALUES (?, ?)').run(Number(classId), Number(studentId));
  return true;
}

function removeClassStudent(classId, studentId) {
  db.prepare('DELETE FROM class_students WHERE class_id = ? AND student_id = ?').run(Number(classId), Number(studentId));
  return true;
}

/** Practice tests **/
function createPracticeTest(teacherId, title, type, prompt, content_json) {
  const info = db.prepare('INSERT INTO practice_tests (teacher_id, title, type, prompt, content_json) VALUES (?, ?, ?, ?, ?)')
    .run(Number(teacherId), title, type, prompt || null, JSON.stringify(content_json || {}));
  const row = db.prepare('SELECT id, title, type, prompt, created_at FROM practice_tests WHERE id = ?').get(info.lastInsertRowid);
  return { id: row.id, title: row.title, type: row.type };
}

function listPracticeTestsByTeacher(teacherId) {
  return db.prepare('SELECT id, title, type, prompt, created_at FROM practice_tests WHERE teacher_id = ? ORDER BY created_at DESC').all(Number(teacherId));
}

function listAllPracticeTests() {
  return db.prepare('SELECT id, title, type, prompt, created_at FROM practice_tests ORDER BY created_at DESC').all();
}

function getPracticeTest(id) {
  const row = db.prepare('SELECT * FROM practice_tests WHERE id = ?').get(Number(id));
  if (!row) return null;
  return {
    id: row.id,
    teacher_id: row.teacher_id,
    title: row.title,
    type: row.type,
    prompt: row.prompt,
    content_json: row.content_json,
    created_at: row.created_at
  };
}

function createPracticeSubmission(testId, studentId, data_json, evaluation_json, score) {
  const info = db.prepare('INSERT INTO practice_submissions (test_id, student_id, data_json, evaluation_json, score) VALUES (?, ?, ?, ?, ?)')
    .run(Number(testId), Number(studentId), data_json ? JSON.stringify(data_json) : null, JSON.stringify(evaluation_json || {}), score == null ? null : Number(score));
  return db.prepare('SELECT * FROM practice_submissions WHERE id = ?').get(info.lastInsertRowid);
}

/** Reports **/
function countsByTeacher(teacherId) {
  const sessions = db.prepare('SELECT COUNT(*) AS n FROM exam_sessions WHERE teacher_id = ?').get(Number(teacherId)).n;
  const upcoming = db.prepare("SELECT COUNT(*) AS n FROM exam_sessions WHERE teacher_id = ? AND date >= date('now')").get(Number(teacherId)).n;
  const rooms = db.prepare('SELECT COUNT(*) AS n FROM exam_rooms').get().n;
  const bookings = db.prepare('SELECT COUNT(*) AS n FROM bookings').get().n;
  const submissions = db.prepare('SELECT COUNT(*) AS n FROM submissions').get().n;
  const avgGradeRow = db.prepare('SELECT AVG(grade_final) AS avg FROM submissions WHERE grade_final IS NOT NULL').get();
  const avgGrade = avgGradeRow && avgGradeRow.avg != null ? Math.round(avgGradeRow.avg) : null;
  return { sessions, upcomingSessions: upcoming, rooms, bookings, submissions, avgGrade };
}

module.exports = {
  createUser,
  getUserByEmail,
  getUserById,
  createRoom,
  listRooms,
  getRoom,
  updateRoom,
  deleteRoom,
  createSession,
  listSessionsByTeacher,
  getSessionByIdForTeacher,
  getSession,
  updateSession,
  listScheduledSessionsFromDate,
  listBookedSeats,
  findExistingBooking,
  createBooking,
  listBookingsByStudent,
  getBookingById,
  createSubmission,
  getSubmission,
  updateSubmissionFields,
  getLastSubmissionByBooking,
  listSubmissions,
  getFallbackExam,
  setFallbackExam,
  createClass,
  listClasses,
  countClassStudents,
  addClassStudent,
  removeClassStudent,
  createPracticeTest,
  listPracticeTestsByTeacher,
  listAllPracticeTests,
  getPracticeTest,
  createPracticeSubmission,
  countsByTeacher
};
