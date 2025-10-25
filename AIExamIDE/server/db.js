const Database = require('better-sqlite3');
const path = require('path');

const DB_PATH = path.join(__dirname, 'exam_system.db');
const db = new Database(DB_PATH);

function migrate() {
  db.exec(`
    PRAGMA journal_mode=DELETE;
    PRAGMA foreign_keys=ON;

    CREATE TABLE IF NOT EXISTS users (
      id INTEGER PRIMARY KEY AUTOINCREMENT,
      email TEXT NOT NULL UNIQUE,
      name TEXT NOT NULL,
      password_hash TEXT NOT NULL,
      role TEXT NOT NULL CHECK(role IN ('teacher','student')),
      created_at TEXT NOT NULL DEFAULT (datetime('now'))
    );

    CREATE TABLE IF NOT EXISTS classes (
      id INTEGER PRIMARY KEY AUTOINCREMENT,
      name TEXT NOT NULL,
      teacher_id INTEGER NOT NULL,
      created_at TEXT NOT NULL DEFAULT (datetime('now')),
      FOREIGN KEY (teacher_id) REFERENCES users(id) ON DELETE CASCADE
    );

    CREATE TABLE IF NOT EXISTS class_students (
      class_id INTEGER NOT NULL,
      student_id INTEGER NOT NULL,
      PRIMARY KEY (class_id, student_id),
      FOREIGN KEY (class_id) REFERENCES classes(id) ON DELETE CASCADE,
      FOREIGN KEY (student_id) REFERENCES users(id) ON DELETE CASCADE
    );

    CREATE TABLE IF NOT EXISTS exam_rooms (
      id INTEGER PRIMARY KEY AUTOINCREMENT,
      name TEXT NOT NULL,
      seatmap_json TEXT NOT NULL,
      created_at TEXT NOT NULL DEFAULT (datetime('now')),
      updated_at TEXT NOT NULL DEFAULT (datetime('now'))
    );

    CREATE TRIGGER IF NOT EXISTS trg_exam_rooms_updated
    AFTER UPDATE ON exam_rooms
    BEGIN
      UPDATE exam_rooms SET updated_at = datetime('now') WHERE id = NEW.id;
    END;

    CREATE TABLE IF NOT EXISTS exam_sessions (
      id INTEGER PRIMARY KEY AUTOINCREMENT,
      teacher_id INTEGER NOT NULL,
      room_id INTEGER NOT NULL,
      title TEXT,
      date TEXT NOT NULL,
      start_time TEXT,
      end_time TEXT,
      exam_type TEXT NOT NULL DEFAULT 'java',
      ai_generated INTEGER NOT NULL DEFAULT 1,
      status TEXT NOT NULL DEFAULT 'scheduled',
      created_at TEXT NOT NULL DEFAULT (datetime('now')),
      updated_at TEXT NOT NULL DEFAULT (datetime('now')),
      FOREIGN KEY (teacher_id) REFERENCES users(id) ON DELETE CASCADE,
      FOREIGN KEY (room_id) REFERENCES exam_rooms(id) ON DELETE RESTRICT
    );

    CREATE TRIGGER IF NOT EXISTS trg_exam_sessions_updated
    AFTER UPDATE ON exam_sessions
    BEGIN
      UPDATE exam_sessions SET updated_at = datetime('now') WHERE id = NEW.id;
    END;

    CREATE TABLE IF NOT EXISTS bookings (
      id INTEGER PRIMARY KEY AUTOINCREMENT,
      session_id INTEGER NOT NULL,
      student_id INTEGER NOT NULL,
      seat_id TEXT NOT NULL,
      status TEXT NOT NULL DEFAULT 'booked',
      created_at TEXT NOT NULL DEFAULT (datetime('now')),
      UNIQUE(session_id, seat_id),
      UNIQUE(session_id, student_id),
      FOREIGN KEY (session_id) REFERENCES exam_sessions(id) ON DELETE CASCADE,
      FOREIGN KEY (student_id) REFERENCES users(id) ON DELETE CASCADE
    );

    CREATE TABLE IF NOT EXISTS submissions (
      id INTEGER PRIMARY KEY AUTOINCREMENT,
      booking_id INTEGER,
      files_json TEXT,
      tasks_json TEXT,
      csvs_json TEXT,
      evaluation_json TEXT,
      grade_final INTEGER,
      feedback_json TEXT,
      created_at TEXT NOT NULL DEFAULT (datetime('now')),
      updated_at TEXT NOT NULL DEFAULT (datetime('now')),
      FOREIGN KEY (booking_id) REFERENCES bookings(id) ON DELETE SET NULL
    );

    CREATE TRIGGER IF NOT EXISTS trg_submissions_updated
    AFTER UPDATE ON submissions
    BEGIN
      UPDATE submissions SET updated_at = datetime('now') WHERE id = NEW.id;
    END;

    CREATE TABLE IF NOT EXISTS practice_tests (
      id INTEGER PRIMARY KEY AUTOINCREMENT,
      teacher_id INTEGER NOT NULL,
      title TEXT NOT NULL,
      type TEXT NOT NULL CHECK(type IN ('ide','mcq')),
      prompt TEXT,
      content_json TEXT,
      created_at TEXT NOT NULL DEFAULT (datetime('now')),
      FOREIGN KEY (teacher_id) REFERENCES users(id) ON DELETE CASCADE
    );

    CREATE TABLE IF NOT EXISTS practice_submissions (
      id INTEGER PRIMARY KEY AUTOINCREMENT,
      test_id INTEGER NOT NULL,
      student_id INTEGER NOT NULL,
      data_json TEXT,
      evaluation_json TEXT,
      score INTEGER,
      created_at TEXT NOT NULL DEFAULT (datetime('now')),
      FOREIGN KEY (test_id) REFERENCES practice_tests(id) ON DELETE CASCADE,
      FOREIGN KEY (student_id) REFERENCES users(id) ON DELETE CASCADE
    );

    CREATE TABLE IF NOT EXISTS fallback_exam (
      id INTEGER PRIMARY KEY CHECK(id = 1),
      json TEXT NOT NULL
    );
  `);
}

function getDb() { return db; }

module.exports = { db, migrate, getDb };
