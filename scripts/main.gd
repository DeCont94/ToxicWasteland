extends Node2D
## Главный скрипт прототипа «Замри»: строит серый уровень, разносит шум,
## сохраняет прогресс в укрытиях и показывает, почему симбиот тебя услышал.

# ---------- Уровень (метры) ----------
const LEVEL_END := 62.0
const FINISH_X := 60.0
# укрытия = точки сохранения
var shelters := [
	{"x": 1.0, "name": "Выход из бункера"},
	{"x": 31.0, "name": "Подъезд"},
]
var zones := [
	{"a": 4.0, "b": 30.0, "name": "Участок 1. Двор: шаг слышен, крадучись — нет"},
	{"a": 33.0, "b": 58.0, "name": "Участок 2. Стекло: замри и не дыши"},
]
var glass := [Vector2(37.5, 40.0), Vector2(46.0, 48.5)]
var walls := [43.0]                       # низкая стенка: шум через неё вдвое тише
var sym_defs := [
	{"x": 18.0, "a": 9.0, "b": 27.0},
	{"x": 50.0, "a": 35.0, "b": 54.0},
]
var pickup_defs := [21.0]                 # запасной фильтр

# ---------- Состояние ----------
var player: Player
var symbiotes: Array = []
var pickups: Array = []                   # {"x":float, "taken":bool}
var rings: Array = []                     # волны шума для отрисовки
var heard_marks: Array = []               # «услышал: …» над симбиотами
var now := 0.0
var paused := false
var debug := false
var finished := false
var fail_timer := 0.0
var fail_reason := ""
var message := ""
var message_timer := 0.0
var noticed := 0
var contacts := 0
var start_time := 0.0
var cp := {}                              # сохранение
var camera: Camera2D
var hud: Node2D


func _ready() -> void:
	_setup_input()
	camera = Camera2D.new()
	add_child(camera)
	camera.make_current()
	camera.zoom = Vector2(0.8, 0.8)          # в кадре ≈ 25 м: симбиота видно раньше, чем он услышит бег
	camera.offset = Vector2(0, -2.6 * Balance.PX_PER_M)
	player = Player.new()
	player.main = self
	add_child(player)
	for d in sym_defs:
		var s := Symbiote.new()
		s.main = self
		add_child(s)
		symbiotes.append(s)
	for px in pickup_defs:
		pickups.append({"x": px, "taken": false})
	var layer := CanvasLayer.new()
	add_child(layer)
	hud = preload("res://scripts/hud.gd").new()
	hud.main = self
	layer.add_child(hud)
	cp = {"x": shelters[0]["x"], "filter": Balance.filter_start, "spares": Balance.spares_start, "taken": [], "index": 0}
	_restore_checkpoint()
	flash_message("Дойди до квартиры справа. Пробел — замри и не дыши.", 6.0)


func _setup_input() -> void:
	var map := {
		"left": [KEY_A, KEY_LEFT], "right": [KEY_D, KEY_RIGHT],
		"run": [KEY_SHIFT], "sneak": [KEY_CTRL, KEY_C],
		"freeze": [KEY_SPACE], "use": [KEY_E], "change_filter": [KEY_R],
		"watch": [KEY_TAB], "debug": [KEY_F1], "restart": [KEY_ENTER],
	}
	for action in map:
		if not InputMap.has_action(action):
			InputMap.add_action(action)
		for key in map[action]:
			var ev := InputEventKey.new()
			ev.physical_keycode = key
			InputMap.action_add_event(action, ev)


func _process(delta: float) -> void:
	if Input.is_action_just_pressed("debug"):
		debug = not debug
	if finished:
		if Input.is_action_just_pressed("restart"):
			get_tree().reload_current_scene()
		_tick_visuals(delta)
		return
	if fail_timer > 0.0:
		fail_timer -= delta
		if fail_timer <= 0.0:
			paused = false
			_restore_checkpoint()
		_tick_visuals(delta)
		return
	now += delta
	# укрытия
	for i in range(shelters.size()):
		var sh: Dictionary = shelters[i]
		if i > cp["index"] and player.x >= sh["x"]:
			_save_checkpoint(i)
	if player.x >= FINISH_X:
		finished = true
		paused = true
	camera.position = Vector2(player.x * Balance.PX_PER_M, 0)
	_tick_visuals(delta)


func _tick_visuals(delta: float) -> void:
	for r in rings:
		r["t"] += delta
	rings = rings.filter(func(r): return r["t"] < 1.2)
	for h in heard_marks:
		h["t"] += delta
	heard_marks = heard_marks.filter(func(h): return h["t"] < 2.5)
	message_timer = maxf(0.0, message_timer - delta)
	queue_redraw()
	hud.queue_redraw()


# ---------- Шум ----------
func make_noise(nx: float, radius: float, label: String) -> void:
	var ring := {"x": nx, "r": radius, "t": 0.0, "label": label, "heard": false}
	for s in symbiotes:
		if s.hear(nx, radius, label, now):
			ring["heard"] = true
	rings.append(ring)


var heard_stats := {}

func on_heard(s: Symbiote, nx: float, radius: float, label: String) -> void:
	noticed += 1
	heard_stats[label] = heard_stats.get(label, 0) + 1
	heard_marks.append({"sym": s, "x": nx, "t": 0.0,
		"text": "услышал: %s (%s м)" % [label, _fmt(radius * Balance.sym_sensitivity * wall_factor_between(s.x, nx))]})


func on_contact(s: Symbiote) -> void:
	contacts += 1
	player.on_contact(s.x)


func wall_factor_between(a: float, b: float) -> float:
	var f := 1.0
	for w in walls:
		if (a - w) * (b - w) < 0.0:
			f *= Balance.wall_factor
	return f


func nearest_symbiote_distance(px: float) -> float:
	var best := 999.0
	for s in symbiotes:
		best = minf(best, absf(s.x - px))
	return best


func on_glass(px: float) -> bool:
	for g in glass:
		if px >= g.x and px <= g.y:
			return true
	return false


func clamp_x(px: float) -> float:
	return clampf(px, 0.3, LEVEL_END)


func try_pickup(p: Player) -> void:
	for pk in pickups:
		if not pk["taken"] and absf(pk["x"] - p.x) <= 1.0:
			if p.spares >= Balance.spares_max:
				flash_message("Больше фильтров не унести.")
				return
			pk["taken"] = true
			p.spares += 1
			flash_message("Запасной фильтр. R — заменить.")
			return


# ---------- Сохранение и провал ----------
func _save_checkpoint(i: int) -> void:
	var taken := []
	for k in range(pickups.size()):
		if pickups[k]["taken"]:
			taken.append(k)
	cp = {"x": shelters[i]["x"], "filter": player.filter_left, "spares": player.spares, "taken": taken, "index": i}
	flash_message("%s. Прогресс сохранён." % shelters[i]["name"])


func _restore_checkpoint() -> void:
	player.reset_to(cp["x"], cp["filter"], cp["spares"])
	for k in range(pickups.size()):
		pickups[k]["taken"] = k in cp["taken"]
	for i in range(symbiotes.size()):
		var d: Dictionary = sym_defs[i]
		symbiotes[i].setup(d["x"], d["a"], d["b"])
	rings.clear()
	heard_marks.clear()
	camera.position = Vector2(player.x * Balance.PX_PER_M, 0)
	camera.reset_smoothing()


func fail(reason: String) -> void:
	if fail_timer > 0.0 or finished:
		return
	fail_reason = reason
	fail_timer = 2.5
	paused = true


func flash_message(text: String, seconds := 3.0) -> void:
	message = text
	message_timer = seconds


func _fmt(v: float) -> String:
	if is_equal_approx(v, roundf(v)):
		return str(int(roundf(v)))
	return "%.1f" % v


# ---------- Отрисовка мира ----------
func _draw() -> void:
	var m := Balance.PX_PER_M
	var font := ThemeDB.fallback_font
	# зоны-участки
	for z in zones:
		draw_rect(Rect2(z["a"] * m, -6.0 * m, (z["b"] - z["a"]) * m, 6.0 * m), Color(1, 1, 1, 0.02))
		draw_string(font, Vector2(z["a"] * m + 8, -5.4 * m), z["name"], HORIZONTAL_ALIGNMENT_LEFT, -1, 16, Color(0.6, 0.65, 0.68))
	# земля
	draw_rect(Rect2(-2.0 * m, 0, (LEVEL_END + 4.0) * m, 6.0 * m), Color(0.1, 0.11, 0.12))
	draw_line(Vector2(-2.0 * m, 0), Vector2((LEVEL_END + 2.0) * m, 0), Color(0.3, 0.32, 0.34), 2.0)
	# укрытия
	for sh in shelters:
		draw_rect(Rect2((sh["x"] - 0.8) * m, -2.4 * m, 1.6 * m, 2.4 * m), Color(0.25, 0.22, 0.15, 0.6))
		draw_string(font, Vector2((sh["x"] - 1.5) * m, -2.6 * m), sh["name"], HORIZONTAL_ALIGNMENT_CENTER, 3.0 * m, 14, Color(0.8, 0.7, 0.45))
	# квартира — финиш
	draw_rect(Rect2(FINISH_X * m, -3.0 * m, 2.0 * m, 3.0 * m), Color(0.3, 0.26, 0.15, 0.7))
	draw_string(font, Vector2((FINISH_X - 0.5) * m, -3.2 * m), "Квартира на Мира", HORIZONTAL_ALIGNMENT_LEFT, -1, 14, Color(0.9, 0.8, 0.5))
	# стекло
	for g in glass:
		draw_rect(Rect2(g.x * m, -6, (g.y - g.x) * m, 6), Color(0.6, 0.8, 0.85, 0.7))
		draw_string(font, Vector2(g.x * m, 22), "битое стекло", HORIZONTAL_ALIGNMENT_LEFT, -1, 13, Color(0.6, 0.8, 0.85))
	# стенка
	for w in walls:
		draw_rect(Rect2(w * m - 6, -1.0 * m, 12, 1.0 * m), Color(0.35, 0.35, 0.38))
	# фильтры
	for pk in pickups:
		if not pk["taken"]:
			draw_rect(Rect2(pk["x"] * m - 8, -16, 16, 16), Color(0.85, 0.75, 0.3))
			if absf(pk["x"] - player.x) <= 1.0:
				draw_string(font, Vector2(pk["x"] * m - 60, -28), "E — взять фильтр", HORIZONTAL_ALIGNMENT_CENTER, 120, 13, Color(0.9, 0.85, 0.6))
	# волны шума
	for r in rings:
		var p: float = clampf(r["t"] / 0.4, 0.0, 1.0)
		var a: float = 1.0 - clampf((r["t"] - 0.4) / 0.8, 0.0, 1.0)
		var col := Color(1.0, 0.35, 0.3, 0.8 * a) if r["heard"] else Color(0.8, 0.85, 0.9, 0.35 * a)
		var center := Vector2(r["x"] * m, -0.9 * m)
		draw_arc(center, maxf(2.0, r["r"] * m * p), 0, TAU, 64, col, 2.0)
	# «услышал: …»
	for h in heard_marks:
		var s: Symbiote = h["sym"]
		var a2: float = 1.0 - clampf((h["t"] - 1.5) / 1.0, 0.0, 1.0)
		var from := s.position
		var to := Vector2(h["x"] * m, -0.9 * m)
		draw_line(from, to, Color(1.0, 0.4, 0.35, 0.7 * a2), 2.0)
		draw_string(font, from + Vector2(-110, -1.0 * m), h["text"], HORIZONTAL_ALIGNMENT_CENTER, 220, 15, Color(1.0, 0.55, 0.5, a2))
