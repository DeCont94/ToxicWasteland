extends Node2D
## Экран поверх игры: подсказки, сообщения, часы деда (Tab), отладка (F1).
## Полосок здоровья и фильтра нет — состояние видно по краям «маски».

var main: Node


func _draw() -> void:
	if main == null or main.player == null:
		return
	var size := get_viewport_rect().size
	var font := ThemeDB.fallback_font
	var p: Player = main.player
	var B := Balance

	# --- края маски темнеют, когда кончается задержка дыхания или воздух ---
	var dark := 0.0
	if p.holding:
		dark = 1.0 - p.hold_left / maxf(0.01, p.hold_capacity())
	if p.suffocating:
		dark = maxf(dark, 1.0 - p.suffocate_left / B.suffocation_time)
	var fog := 0.0
	if p.filter_left < B.filter_full * 0.10:
		fog = 0.25
	if p.mask_on:
		_vignette(size, 0.35 + 0.5 * dark, fog)
	elif p.suffocating:
		_vignette(size, 0.5 * dark, 0.0)

	# --- сообщение ---
	if main.message_timer > 0.0:
		var a: float = clampf(main.message_timer, 0.0, 1.0)
		draw_string(font, Vector2(0, 90), main.message, HORIZONTAL_ALIGNMENT_CENTER, size.x, 22, Color(0.9, 0.9, 0.85, a))

	# --- часы деда (Tab) ---
	if Input.is_action_pressed("watch"):
		var c := Vector2(size.x - 120, size.y - 130)
		draw_circle(c, 70, Color(0.12, 0.12, 0.1, 0.95))
		draw_arc(c, 70, 0, TAU, 48, Color(0.6, 0.55, 0.4), 3.0)
		draw_string(font, c + Vector2(-60, -10), "03:52", HORIZONTAL_ALIGNMENT_CENTER, 120, 26, Color(0.9, 0.85, 0.7))
		var share := p.filter_left / B.filter_full
		var col := Color(0.3, 0.8, 0.35)
		if share < 0.25:
			col = Color(0.85, 0.25, 0.2)
		elif share < 0.5:
			col = Color(0.9, 0.75, 0.2)
		draw_circle(c + Vector2(0, 30), 12, col)
		draw_string(font, c + Vector2(-60, 62), "запасных: %d" % p.spares, HORIZONTAL_ALIGNMENT_CENTER, 120, 14, Color(0.8, 0.8, 0.75))

	# --- подсказка управления ---
	var hint := "A/D идти · Shift бег · Ctrl крадучись · Пробел замри · E взять · R фильтр · Tab часы · F1 числа"
	if "hint_text" in main:
		hint = main.hint_text
	draw_string(font, Vector2(0, size.y - 16), hint, HORIZONTAL_ALIGNMENT_CENTER, size.x, 14, Color(0.55, 0.58, 0.6))

	# --- отладка (F1) ---
	if main.debug:
		var lines := [
			"фильтр: %.0f с (запасных %d)" % [p.filter_left, p.spares],
			"задержка дыхания: %.1f / %.1f с%s" % [p.hold_left, p.hold_capacity(), "  (не дышит)" if p.holding else ""],
			"режим: %s%s" % [p.mode, "  · одышка" if p.winded > 0.0 else ""],
			"ближайший симбиот: %.1f м" % main.nearest_symbiote_distance(p.x),
			"позиция: %.1f м" % p.x,
			"услышали тебя: %d раз · контактов: %d" % [main.noticed, main.contacts],
			"режим контакта: %s (balance.gd)" % B.contact_mode,
		]
		if p.suffocating:
			lines.append("УДУШЬЕ: %.1f с" % p.suffocate_left)
		for i in range(lines.size()):
			draw_string(font, Vector2(16, 28 + i * 20), lines[i], HORIZONTAL_ALIGNMENT_LEFT, -1, 15, Color(0.75, 0.9, 0.8))

	# --- провал ---
	if main.fail_timer > 0.0:
		draw_rect(Rect2(Vector2.ZERO, size), Color(0, 0, 0, 0.75))
		draw_string(font, Vector2(0, size.y / 2), main.fail_reason, HORIZONTAL_ALIGNMENT_CENTER, size.x, 30, Color(0.85, 0.8, 0.75))
		draw_string(font, Vector2(0, size.y / 2 + 40), "Назад к последнему укрытию…", HORIZONTAL_ALIGNMENT_CENTER, size.x, 18, Color(0.6, 0.6, 0.6))

	# --- записка деда ---
	if "note_open" in main and main.note_open:
		draw_rect(Rect2(Vector2.ZERO, size), Color(0, 0, 0, 0.55))
		var pw := minf(640.0, size.x - 64.0)
		var px := (size.x - pw) / 2.0
		draw_rect(Rect2(px, 150, pw, 320), Color(0.78, 0.75, 0.66))
		draw_multiline_string(font, Vector2(px + 32, 200), main.NOTE, HORIZONTAL_ALIGNMENT_LEFT, pw - 64, 20, -1, Color(0.12, 0.1, 0.08))
		draw_string(font, Vector2(0, 500), "E — дальше", HORIZONTAL_ALIGNMENT_CENTER, size.x, 15, Color(0.7, 0.7, 0.7))

	# --- финиш Пролога ---
	if main.finished and main.has_method("finish_lines"):
		var ta: float = clampf(main.title_t / 2.5, 0.0, 1.0)
		var tb: float = clampf((main.title_t - 3.0) / 1.5, 0.0, 1.0)
		draw_string(font, Vector2(0, size.y * 0.32), "ХИМЕРА", HORIZONTAL_ALIGNMENT_CENTER, size.x, 88, Color(0.75, 0.95, 0.9, ta))
		draw_string(font, Vector2(0, size.y * 0.32 + 50), "Пролог", HORIZONTAL_ALIGNMENT_CENTER, size.x, 22, Color(0.7, 0.8, 0.78, tb))
		draw_string(font, Vector2(0, size.y - 60), "Enter — сыграть ещё раз", HORIZONTAL_ALIGNMENT_CENTER, size.x, 16, Color(0.6, 0.6, 0.6, tb))
		return

	# --- финиш ---
	if main.finished:
		draw_rect(Rect2(Vector2.ZERO, size), Color(0, 0, 0, 0.8))
		var y := size.y / 2 - 60
		draw_string(font, Vector2(0, y), "Квартира на Мира. Дошёл.", HORIZONTAL_ALIGNMENT_CENTER, size.x, 32, Color(0.9, 0.85, 0.6))
		draw_string(font, Vector2(0, y + 50), "Время: %d с · фильтра осталось: %d с · запасных: %d" % [int(main.now), int(p.filter_left), p.spares], HORIZONTAL_ALIGNMENT_CENTER, size.x, 18, Color(0.8, 0.8, 0.8))
		draw_string(font, Vector2(0, y + 80), "Тебя услышали %d раз, контактов с роем: %d" % [main.noticed, main.contacts], HORIZONTAL_ALIGNMENT_CENTER, size.x, 18, Color(0.8, 0.8, 0.8))
		draw_string(font, Vector2(0, y + 130), "Enter — сыграть ещё раз", HORIZONTAL_ALIGNMENT_CENTER, size.x, 16, Color(0.6, 0.6, 0.6))


func _vignette(size: Vector2, strength: float, fog: float) -> void:
	# простая «маска»: тёмные полосы по краям, чем сильнее — тем шире
	var steps := 24
	for i in range(steps):
		var k := float(i) / steps
		var w := size.x * 0.18 * strength * (1.0 - k)
		var h := size.y * 0.18 * strength * (1.0 - k)
		var col := Color(0, 0, 0, 0.045)
		draw_rect(Rect2(0, 0, w, size.y), col)
		draw_rect(Rect2(size.x - w, 0, w, size.y), col)
		draw_rect(Rect2(0, 0, size.x, h), col)
		draw_rect(Rect2(0, size.y - h, size.x, h), col)
	if fog > 0.0:
		draw_rect(Rect2(Vector2.ZERO, size), Color(0.7, 0.75, 0.75, fog))
