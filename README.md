# Abyssal Labyrinth - 3D First-Person Dungeon Crawler (DRPG)

Prototipe game **3D First-Person Turn-Based Dungeon Crawler (DRPG)** dengan estetika retro dark-fantasy dan mekanik pertarungan **Active QTE Engine** yang terinspirasi oleh judul-judul klasik (Wizardry / King's Field) serta modern turn-based QTE (*Clair Obscur: Expedition 33*).

---

## 🛠 Spesifikasi Teknis
- **Engine**: Unity 6
- **Versi Unity**: `Unity 6000.3.16f1`
- **Render Pipeline**: Universal Render Pipeline (URP) / Built-in compatible
- **Input System**: Mendukung Unity New Input System & Legacy Input Manager

---

## 🎮 Skema Kontrol (PC / Keyboard & Mouse)

### Eksplorasi (Dungeon Exploration)
| Tombol | Fungsi |
| :--- | :--- |
| **W** / **Panah Atas** *(Tahan)* | Melangkah maju kontinu (Grid Step 2 unit) |
| **S** / **Panah Bawah** *(Tahan)* | Melangkah mundur kontinu (Grid Step 2 unit) |
| **A** / **Panah Kiri** | Berbelok 90° ke kiri secara diskret |
| **D** / **Panah Kanan** | Berbelok 90° ke kanan secara diskret |
| **E** | Interaksi dengan objek dunia (Ambil Kunci, Periksa Pintu Terkunci) |

### Pertarungan (Turn-Based Active QTE Combat)
| Aksi | Kontrol |
| :--- | :--- |
| **Pilih Menu Aksi** | Klik tombol **ATTACK** (Serangan Fisik) atau **CAST** (Sihir Elemen) |
| **Serangan Fisik (Timing Bar)** | Tekan **Spasi** atau **Klik Kiri** saat kursor timing berada di Sweet Spot (Good / Perfect) |
| **Rapalan Mantra Sihir (Typing QTE)** | Ketik kata mantra alfabetik (misal: `IGNIS`, `MORTIS`, `FULGUR`) sebelum waktu habis |
| **Bertahan / Parry (Defend QTE)** | Tekan **Spasi** tepat saat serangan musuh/boss mencapai zona target untuk memicu Parry (0 damage) atau Guard (50% damage) |
| **Multi-Hit Flurry Defense** | Tekan **Spasi** berulang secara berirama untuk menangkis tebasan beruntun dari Boss |

---

## 🏗 Menu Editor Rebuild (Level & UI)
Game ini dilengkapi generator prosedural dan tool otomatisasi editor untuk membangun ulang seluruh dungeon atau kanvas UI hanya dengan 1 klik:

1. **Rebuild Dungeon & Level Geometri**:
   - Buka menu di Unity Editor: `DungeonCrawler` -> `Build Complete Dungeon`.
   - Tool ini akan:
     - Membuat tata letak dungeon (Ruang Awal, Koridor, Ruang Musuh, Ruang Kunci, Pintu Tersegel, dan Arena Boss).
     - Menghasilkan material prosedural retro (dinding batu bata, lantai ubin, atap, pintu kayu, dan obor berkedip).
     - Memposisikan spawn musuh Skeleton, Boss Abyssal Executioner, dan Kunci Dungeon.
     - Mengatur pencahayaan atmosferik gelap dan fog pekat.
     - Menyinkronkan sistem auto-map Fog of War.

2. **Rebuild Canvas & HUD UI**:
   - Buka menu di Unity Editor: `DungeonCrawler` -> `Build Combat UI`.
   - Tool ini akan:
     - Membangun seluruh hierarchy UI responsif (HUD Eksplorasi, Minimap 7x7, Panel Combat QTE, Layar Game Over, dan Layar Victory).
     - Menghubungkan event listener ke `CombatManager` dan `HUDManager`.
