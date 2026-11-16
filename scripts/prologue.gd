extends Node2D
## ПРОЛОГ по канону (выпуск 1): угасающий бункер → мастерская деда →
## стена корней у гермодвери (первый выстрел) → туман, маска → шахта → выход в город.
## Тон — только свет, тени и звук. Боя нет.

const M := Balance.PX_PER_M
const CEIL := 3.2              # высота потолка бункера, м
const BENCH_X := 19.0          # верстак деда
const GRANDPA_X := 18.2
const ROOTS_A := 27.4          # стена корней
const ROOTS_B := 28.6
const DOOR_X := 30.0           # гермодверь
const SHAFT_END := 42.0        # конец шахты, дальше улица
const TITLE_X := 58.0
const LEVEL_END := 64.0

# Прощальная записка деда — дословно по канону (запись 10).
const NOTE := "«Моё время вышло. Я закончил Дефлотатор — он расщепляет корку Цветения. Надевай маску. Фильтра хватит ненадолго — ищи новые у павших оперативников или в ящиках снабжения. Найди комплекс „Альфа“. Там есть ответ… или хотя бы чистый воздух. Прости меня.»"

# ---------- состояние, которое читают Player и HUD ----------
var player: Player
var paused := false
var debug := false
var finished := false
var fail_timer := 0.0
var fail_reason := ""
var message := ""
var message_timer := 0.0
var noticed := 0
var contacts := 0
var now := 0.0
var note_open := false
var hint_text := "A/D идти · Shift бег · Ctrl крадучись · Пробел замри · E действие · ЛКМ Дефлотатор · F1 числа"

# ---------- сюжетные флаги ----------
var took_items := false
var roots_hp := 1              # тонкие корни Пролога: один импульс
var roots_dust := 0.0          # 0 — целы; >0 — рассыпаются
var fog_started := false
var fog_front := 99.0          # туман занимает всё правее этой точки
var title_t := 0.0
var alarm := 0                 # тревога роя (выстрелы)

# ---------- Дефлотатор ----------
var heat := 0.0
var overheated := 0.0
var beam_t := 0.0              # идёт импульс
var beam_dir := Vector2.RIGHT

# ---------- узлы ----------
var camera: Camera2D
var hud: Node2D
var glow: Node2D               # всё, что светится само (не темнеет в полярной ночи)
var city_layer: Node2D         # небо и город за шахтой
var lamps: Array = []          # {"light":PointLight2D, "base":float, "seed":float}
var bud_light: PointLight2D
var roots_light: PointLight2D
var outside_light: PointLight2D
var bench_light: PointLight2D
var dust: CPUParticles2D
var fog: CPUParticles2D
var spores: CPUParticles2D
var snd := {}                  # имя → AudioStreamPlayer
var breath_heavy_stream: AudioStreamWAV
var breath_calm_stream: AudioStreamWAV
var cough_cd := 0.0


func _ready() -> void:
	_setup_input()
	var cm := CanvasModulate.new()
	cm.color = Color(0.16, 0.15, 0.17)
	add_child(cm)

	var back := Node2D.new()
	back.set_script(preload("res://scripts/glow_layer.gd"))
	back.level = self
	back.layer = "back"
	var unshaded_back := CanvasItemMaterial.new()
	unshaded_back.light_mode = CanvasItemMaterial.LIGHT_MODE_UNSHADED
	back.material = unshaded_back
	back.z_index = -1
	add_child(back)
	city_layer = back

	glow = Node2D.new()
	glow.set_script(preload("res://scripts/glow_layer.gd"))
	glow.level = self
	var unshaded := CanvasItemMaterial.new()
	unshaded.light_mode = CanvasItemMaterial.LIGHT_MODE_UNSHADED
	glow.material = unshaded
	glow.z_index = 1
	add_child(glow)

	player = Player.new()
	player.main = self
	player.silhouette = true
	player.z_index = 2
	add_child(player)
	player.reset_to(1.5, Balance.filter_full, 0)
	player.mask_on = false

	camera = Camera2D.new()
	camera.offset = Vector2(0, -1.9 * M)
	camera.position_smoothing_enabled = true
	camera.position_smoothing_speed = 4.0
	add_child(camera)
	camera.make_current()

	_make_lights()
	_make_particles()
	_make_sounds()

	var layer := CanvasLayer.new()
	add_child(layer)
	hud = preload("res://scripts/hud.gd").new()
	hud.main = self
	layer.add_child(hud)
	flash_message("Свет почти погас. Надо выбираться.", 5.0)


func _setup_input() -> void:
	var keys := {
		"left": [KEY_A, KEY_LEFT], "right": [KEY_D, KEY_RIGHT],
		"run": [KEY_SHIFT], "sneak": [KEY_CTRL, KEY_C],
		"freeze": [KEY_SPACE], "use": [KEY_E], "change_filter": [KEY_R],
		"watch": [KEY_TAB], "debug": [KEY_F1], "restart": [KEY_ENTER],
	}
	for action in keys:
		if not InputMap.has_action(action):
			InputMap.add_action(action)
			for key in keys[action]:
				var ev := InputEventKey.new()
				ev.physical_keycode = key
				InputMap.action_add_event(action, ev)
	if not InputMap.has_action("shoot"):
		InputMap.add_action("shoot")
		var mb := InputEventMouseButton.new()
		mb.button_index = MOUSE_BUTTON_LEFT
		InputMap.action_add_event("shoot", mb)


# ---------- свет ----------
func _light(x: float, y: float, col: Color, energy: float, scale_m: float) -> PointLight2D:
	var l := PointLight2D.new()
	l.texture = Synth.light_texture()
	l.position = Vector2(x * M, -y * M)
	l.color = col
	l.energy = energy
	l.texture_scale = scale_m * M / 128.0
	add_child(l)
	return l


func _make_lights() -> void:
	# аварийные лампы: тревожный красный, мигают
	for lx in [3.0, 9.5, 24.5, 34.0, 39.0]:
		var l := _light(lx, CEIL - 0.25, Color(1.0, 0.18, 0.12), 1.4, 7.0)
		lamps.append({"light": l, "base": 1.4, "seed": lx * 1.7})
	# лампа над верстаком: тусклая, тёплая
	bench_light = _light(BENCH_X, 2.6, Color(1.0, 0.75, 0.45), 0.9, 6.0)
	# бутоны на теле деда: бирюзовое свечение, дышит в такт Артёму
	bud_light = _light(GRANDPA_X, 1.0, Color(0.3, 1.0, 0.85), 0.5, 3.0)
	# стена корней: пульсирующий красный
	roots_light = _light((ROOTS_A + ROOTS_B) / 2.0, 1.4, Color(1.0, 0.25, 0.2), 1.2, 6.0)
	# свет Цветения с улицы
	outside_light = _light(SHAFT_END + 8.0, 4.0, Color(0.25, 0.95, 0.8), 1.6, 26.0)


func _make_particles() -> void:
	# серая пыль от рассыпавшихся корней
	dust = CPUParticles2D.new()
	dust.position = Vector2((ROOTS_A + ROOTS_B) / 2.0 * M, -1.5 * M)
	dust.emitting = false
	dust.one_shot = true
	dust.amount = 220
	dust.lifetime = 2.6
	dust.explosiveness = 0.85
	dust.emission_shape = CPUParticles2D.EMISSION_SHAPE_RECTANGLE
	dust.emission_rect_extents = Vector2(0.6 * M, 1.5 * M)
	dust.direction = Vector2(0, 1)
	dust.spread = 60.0
	dust.gravity = Vector2(0, 90)
	dust.initial_velocity_min = 10.0
	dust.initial_velocity_max = 60.0
	dust.scale_amount_min = 0.3
	dust.scale_amount_max = 0.7
	dust.color = Color(0.55, 0.55, 0.55, 0.8)
	dust.texture = Synth.light_texture(16)
	add_child(dust)
	# зеленоватый туман: течёт из-за гермодвери влево
	fog = CPUParticles2D.new()
	fog.position = Vector2(DOOR_X * M, -1.3 * M)
	fog.emitting = false
	fog.amount = 160
	fog.lifetime = 7.0
	fog.emission_shape = CPUParticles2D.EMISSION_SHAPE_RECTANGLE
	fog.emission_rect_extents = Vector2(0.4 * M, 1.4 * M)
	fog.direction = Vector2(-1, 0)
	fog.spread = 25.0
	fog.gravity = Vector2.ZERO
	fog.initial_velocity_min = 50.0
	fog.initial_velocity_max = 110.0
	fog.damping_min = 6.0
	fog.damping_max = 12.0
	fog.scale_amount_min = 1.6
	fog.scale_amount_max = 3.6
	var g := Gradient.new()
	g.set_color(0, Color(0.45, 0.75, 0.5, 0.0))
	g.set_color(1, Color(0.45, 0.75, 0.5, 0.0))
	g.add_point(0.25, Color(0.45, 0.75, 0.5, 0.14))
	g.add_point(0.75, Color(0.4, 0.7, 0.5, 0.09))
	fog.color_ramp = g
	fog.texture = Synth.light_texture(64)
	var fm := CanvasItemMaterial.new()
	fm.light_mode = CanvasItemMaterial.LIGHT_MODE_UNSHADED
	fog.material = fm
	fog.z_index = 3
	add_child(fog)
	# споры на улице: медленно плывут, светятся
	spores = CPUParticles2D.new()
	spores.position = Vector2((SHAFT_END + 11.0) * M, -4.0 * M)
	spores.amount = 90
	spores.lifetime = 9.0
	spores.preprocess = 9.0
	spores.emission_shape = CPUParticles2D.EMISSION_SHAPE_RECTANGLE
	spores.emission_rect_extents = Vector2(13.0 * M, 4.0 * M)
	spores.direction = Vector2(0.3, -1)
	spores.spread = 50.0
	spores.gravity = Vector2(4, -3)
	spores.initial_velocity_min = 3.0
	spores.initial_velocity_max = 12.0
	spores.scale_amount_min = 0.15
	spores.scale_amount_max = 0.35
	var sg := Gradient.new()
	sg.set_color(0, Color(0.4, 1.0, 0.85, 0.0))
	sg.set_color(1, Color(1.0, 0.8, 0.4, 0.0))
	sg.add_point(0.5, Color(0.5, 1.0, 0.85, 0.85))
	spores.color_ramp = sg
	spores.texture = Synth.light_texture(32)
	var spm := CanvasItemMaterial.new()
	spm.light_mode = CanvasItemMaterial.LIGHT_MODE_UNSHADED
	spores.material = spm
	spores.z_index = 3
	add_child(spores)


# ---------- звук ----------
func _player(name: String, stream: AudioStream, db := 0.0) -> AudioStreamPlayer:
	var p := AudioStreamPlayer.new()
	p.stream = stream
	p.volume_db = db
	add_child(p)
	snd[name] = p
	return p


func _make_sounds() -> void:
	_player("hum", Synth.hum(), -8.0).play()
	breath_calm_stream = Synth.breath(3.2, false)
	breath_heavy_stream = Synth.breath(1.7, true)
	_player("breath", breath_calm_stream, -6.0)
	_player("step", Synth.step(), -10.0)
	_player("crackle", Synth.crackle(), -2.0)
	_player("beam", Synth.beam(), -6.0)
	_player("hiss", Synth.hiss(2.5), -6.0)
	_player("cough", Synth.cough(), -4.0)


func play_step(mode: String) -> void:
	var p: AudioStreamPlayer = snd["step"]
	p.pitch_scale = randf_range(0.85, 1.15) * (0.8 if mode == "sneak" else 1.0)
	p.volume_db = {"sneak": -20.0, "walk": -11.0, "run": -6.0}.get(mode, -11.0)
	p.play()


# ---------- кадр ----------
func _process(delta: float) -> void:
	if Input.is_action_just_pressed("debug"):
		debug = not debug
	now += delta
	message_timer = maxf(0.0, message_timer - delta)
	cough_cd = maxf(0.0, cough_cd - delta)

	if note_open:
		if Input.is_action_just_pressed("use") or Input.is_action_just_pressed("freeze") or Input.is_action_just_pressed("shoot"):
			note_open = false
			paused = false
			flash_message("Дефлотатор и противогаз — с собой.", 3.5)
	elif finished:
		title_t += delta
		if Input.is_action_just_pressed("restart"):
			get_tree().reload_current_scene()
	elif fail_timer > 0.0:
		fail_timer -= delta
		if fail_timer <= 0.0:
			paused = false
			player.reset_to(24.0, Balance.filter_full, 0)
			player.mask_on = false
			player.silhouette = true
			flash_message("E — надеть противогаз.", 4.0)
	else:
		_story(delta)
		_deflotator(delta)

	_animate_lights(delta)
	_update_sound()
	# на улице камера отъезжает и смотрит выше: виден город
	var out: float = clampf((player.x - SHAFT_END + 1.0) / 5.0, 0.0, 1.0)
	camera.offset = Vector2(0, lerpf(-1.9, -3.6, out) * M)
	var z: float = lerpf(1.0, 0.72, out)
	camera.zoom = Vector2(z, z)
	camera.position = Vector2(clampf(player.x, 8.0, LEVEL_END - 10.0) * M, 0)
	glow.queue_redraw()
	city_layer.queue_redraw()
	queue_redraw()
	hud.queue_redraw()


func _story(delta: float) -> void:
	# туман ползёт влево
	if fog_started:
		fog_front = maxf(21.0, fog_front - 1.6 * delta)
		if not player.mask_on and in_fog(player.x) and message_timer <= 0.0:
			flash_message("E — надеть противогаз.", 2.0)
	if roots_dust > 0.0:
		roots_dust += delta
		if roots_dust > 1.3 and not fog_started:
			fog_started = true
			fog_front = DOOR_X
			fog.emitting = true
			snd["hiss"].play()
	# выход на улицу и название
	if player.x >= TITLE_X and not finished:
		finished = true
		paused = true
		player.mode = "still"


func _deflotator(delta: float) -> void:
	overheated = maxf(0.0, overheated - delta)
	heat = maxf(0.0, heat - 10.0 * delta)
	if beam_t > 0.0:
		beam_t -= delta
		_beam_hit()
	if not has_deflotator() or paused:
		return
	if Input.is_action_just_pressed("shoot") and beam_t <= 0.0 and overheated <= 0.0:
		beam_t = 1.0
		beam_dir = (aim_point() - _hand()).normalized()
		heat += 34.0
		alarm += 1
		snd["beam"].play()
		make_noise(player.x, 20.0, "импульс Дефлотатора")
		if heat >= 100.0:
			overheated = 8.0
			heat = 100.0
			snd["hiss"].play()
			flash_message("Колба перегрелась.", 2.5)


func _hand() -> Vector2:
	return player.position + Vector2(0.25 * M * player.facing, -1.25 * M)


func _beam_hit() -> void:
	if roots_hp <= 0:
		return
	# луч длиной 8 м: задел ли он стену корней
	var from := _hand()
	var to := from + beam_dir * 8.0 * M
	var steps := 24
	for i in range(steps + 1):
		var p := from.lerp(to, float(i) / steps)
		if p.x >= ROOTS_A * M and p.x <= ROOTS_B * M and p.y <= 0.0 and p.y >= -CEIL * M:
			roots_hp -= 1
			if roots_hp <= 0:
				roots_dust = 0.001
				dust.emitting = true
				snd["crackle"].play()
			return


func _animate_lights(delta: float) -> void:
	for l in lamps:
		var lt: PointLight2D = l["light"]
		var s: float = l["seed"]
		var flick := 0.75 + 0.25 * sin(now * 2.3 + s) * sin(now * 7.1 + s * 3.0)
		if fmod(now * 0.9 + s, 6.0) < 0.12:
			flick = 0.15      # лампа на миг гаснет
		lt.energy = l["base"] * flick
	# бутоны деда повторяют ритм дыхания Артёма
	var period := 1.7 if player.winded > 0.0 else 3.2
	var b := 0.5 + 0.5 * sin(now * TAU / period)
	if player.holding:
		b = 0.15
	bud_light.energy = 0.25 + 0.5 * b
	# корни пульсируют, потом гаснут
	if roots_dust > 0.0:
		roots_light.energy = maxf(0.0, 1.2 - roots_dust)
	else:
		roots_light.energy = 0.9 + 0.4 * sin(now * 3.0)
	bench_light.energy = 0.8 + 0.08 * sin(now * 13.0) * sin(now * 3.0)


func _update_sound() -> void:
	var outside: float = clampf((player.x - SHAFT_END + 2.0) / 6.0, 0.0, 1.0)
	var hum: AudioStreamPlayer = snd["hum"]
	hum.volume_db = lerpf(-8.0, -30.0, outside)
	hum.pitch_scale = lerpf(1.0, 0.6, outside)
	var br: AudioStreamPlayer = snd["breath"]
	if player.mask_on and not player.holding and not paused:
		var want := breath_heavy_stream if (player.winded > 0.0 or player.suffocating) else breath_calm_stream
		if br.stream != want:
			br.stream = want
			br.play()
		elif not br.playing:
			br.play()
	elif br.playing:
		br.stop()


# ---------- интерфейс, который зовёт Player ----------
func make_noise(_nx: float, _radius: float, label: String) -> void:
	if label == "кашель" and cough_cd <= 0.0:
		snd["cough"].play()
		cough_cd = 1.0


func nearest_symbiote_distance(_px: float) -> float:
	return 999.0


func clamp_x(px: float) -> float:
	var hi := LEVEL_END
	if roots_hp > 0:
		hi = ROOTS_A - 0.3
	return clampf(px, 0.4, hi)


func on_glass(_px: float) -> bool:
	return false


func in_fog(px: float) -> bool:
	return fog_started and px >= fog_front and px <= SHAFT_END + 1.0


func has_deflotator() -> bool:
	return took_items


func aim_point() -> Vector2:
	return get_global_mouse_position()


func try_pickup(p: Player) -> void:
	if not took_items and absf(p.x - BENCH_X) <= 1.6:
		took_items = true
		note_open = true
		paused = true
		return
	if took_items and not p.mask_on and fog_started:
		p.mask_on = true
		p.suffocating = false
		p.filter_left = 150.0
		flash_message("Дыхание стало гулким.", 3.0)


func fail(reason: String) -> void:
	if fail_timer > 0.0:
		return
	fail_reason = reason
	fail_timer = 2.5
	paused = true


func flash_message(text: String, seconds := 3.0) -> void:
	message = text
	message_timer = seconds


func finish_lines() -> Array:
	return []


# ---------- мир (то, что освещают лампы) ----------
func _draw() -> void:
	var wall := Color(0.32, 0.33, 0.36)
	var dark := Color(0.12, 0.12, 0.14)
	var metal := Color(0.22, 0.23, 0.25)
	# стены бункера и шахты
	draw_rect(Rect2(-2 * M, -CEIL * M, (SHAFT_END + 2.0) * M, CEIL * M), wall)
	# панели и стыки
	var x := -2.0
	while x < SHAFT_END:
		draw_line(Vector2(x * M, -CEIL * M), Vector2(x * M, 0), dark, 3.0)
		draw_circle(Vector2((x + 0.15) * M, -0.3 * M), 3, dark)
		draw_circle(Vector2((x + 0.15) * M, -(CEIL - 0.3) * M), 3, dark)
		x += 2.4
	# пол и потолок
	draw_rect(Rect2(-2 * M, 0, (LEVEL_END + 6.0) * M, 4.0 * M), Color(0.06, 0.06, 0.07))
	# толща грунта над бункером: закрывает небо, пока Артём под землёй
	draw_rect(Rect2(-30 * M, -(CEIL + 30.0) * M, (SHAFT_END + 30.0) * M, 30.0 * M), Color(0.02, 0.02, 0.025))
	# рваный край выхода из шахты
	draw_colored_polygon(PackedVector2Array([Vector2(SHAFT_END * M, -(CEIL + 30.0) * M), Vector2((SHAFT_END + 0.6) * M, -(CEIL + 30.0) * M),
		Vector2((SHAFT_END + 0.9) * M, -(CEIL + 0.6) * M), Vector2((SHAFT_END + 0.3) * M, -(CEIL - 0.4) * M),
		Vector2((SHAFT_END + 0.5) * M, -1.6 * M), Vector2((SHAFT_END + 0.1) * M, -0.9 * M), Vector2(SHAFT_END * M, 0)]), Color(0.02, 0.02, 0.025))
	# трубы под потолком
	for i in range(3):
		var py := -(CEIL - 0.25 - i * 0.22) * M
		draw_line(Vector2(-2 * M, py), Vector2(SHAFT_END * M, py), metal, 9.0 - i * 2.0)
	for px in [2.0, 7.0, 12.0, 26.0, 33.0, 37.5]:
		draw_rect(Rect2(px * M, -CEIL * M, 10, 0.7 * M), metal)
	# переборки отсеков
	for bx in [6.2, 13.2]:
		draw_rect(Rect2(bx * M, -CEIL * M, 0.35 * M, 0.55 * M), dark)
		draw_rect(Rect2(bx * M, -2.65 * M, 0.08 * M, 2.65 * M), dark)
		draw_rect(Rect2((bx + 0.27) * M, -2.65 * M, 0.08 * M, 2.65 * M), dark)
	# мастерская: полки, верстак, предметы
	draw_rect(Rect2(15.0 * M, -2.4 * M, 2.4 * M, 0.08 * M), dark)
	draw_rect(Rect2(15.0 * M, -1.8 * M, 2.4 * M, 0.08 * M), dark)
	for k in range(6):
		draw_rect(Rect2((15.2 + k * 0.38) * M, -2.4 * M - 18 - (k % 3) * 6, 14, 18 + (k % 3) * 6), metal)
	draw_rect(Rect2((BENCH_X - 0.4) * M, -0.95 * M, 2.4 * M, 0.12 * M), Color(0.2, 0.17, 0.13))
	draw_rect(Rect2((BENCH_X - 0.3) * M, -0.85 * M, 0.08 * M, 0.85 * M), Color(0.15, 0.12, 0.1))
	draw_rect(Rect2((BENCH_X + 1.8) * M, -0.85 * M, 0.08 * M, 0.85 * M), Color(0.15, 0.12, 0.1))
	if not took_items:
		draw_rect(Rect2((BENCH_X + 0.2) * M, -1.05 * M, 0.7 * M, 0.1 * M), metal)          # Дефлотатор
		draw_circle(Vector2((BENCH_X + 1.3) * M, -1.05 * M), 0.12 * M, dark)                # противогаз
		draw_rect(Rect2((BENCH_X + 1.55) * M, -0.99 * M, 0.25 * M, 0.04 * M), Color(0.7, 0.68, 0.6))  # записка
	# дед за верстаком: неподвижный силуэт на стуле
	var ink := Color(0.03, 0.03, 0.035)
	var gx := GRANDPA_X * M
	draw_rect(Rect2(gx - 0.3 * M, -0.5 * M, 0.5 * M, 0.08 * M), ink)                       # сиденье
	draw_line(Vector2(gx - 0.25 * M, -0.5 * M), Vector2(gx - 0.25 * M, 0), ink, 5)
	draw_line(Vector2(gx + 0.15 * M, -0.5 * M), Vector2(gx + 0.15 * M, 0), ink, 5)
	var torso := PackedVector2Array([Vector2(gx - 0.3 * M, -0.52 * M), Vector2(gx + 0.15 * M, -0.52 * M),
		Vector2(gx + 0.32 * M, -1.15 * M), Vector2(gx - 0.05 * M, -1.2 * M)])
	draw_colored_polygon(torso, ink)
	draw_circle(Vector2(gx + 0.25 * M, -1.25 * M), 0.14 * M, ink)                           # голова склонена
	draw_line(Vector2(gx + 0.2 * M, -1.05 * M), Vector2(gx + 0.75 * M, -0.95 * M), ink, 0.1 * M)  # рука на верстаке
	draw_line(Vector2(gx + 0.15 * M, -0.5 * M), Vector2(gx + 0.55 * M, -0.5 * M), ink, 0.14 * M)
	draw_line(Vector2(gx + 0.55 * M, -0.5 * M), Vector2(gx + 0.55 * M, 0), ink, 0.12 * M)
	# гермодверь: распахнутое тяжёлое кольцо-люк
	draw_rect(Rect2((DOOR_X - 0.2) * M, -2.8 * M, 0.4 * M, 2.8 * M), Color(0.18, 0.19, 0.2))
	draw_arc(Vector2((DOOR_X + 0.9) * M, -1.4 * M), 1.1 * M, -PI / 2, PI / 2, 24, Color(0.2, 0.21, 0.22), 0.25 * M)
	# ржавая шахта: косые подпорки, обрывки кабеля
	for sx in [32.0, 35.0, 38.0, 41.0]:
		draw_line(Vector2(sx * M, -CEIL * M), Vector2((sx + 0.8) * M, 0), Color(0.24, 0.17, 0.12), 8)
		draw_line(Vector2((sx + 0.3) * M, -CEIL * M), Vector2((sx + 0.5) * M, -2.2 * M), dark, 2)
	# улица: силуэты (их подсвечивает свет Цветения)
	var street := Color(0.07, 0.08, 0.09)
	draw_rect(Rect2(SHAFT_END * M, -0.2 * M, (LEVEL_END - SHAFT_END + 6) * M, 0.2 * M), street)
	# обломок трубы и сугробы у выхода
	draw_rect(Rect2((SHAFT_END + 1.0) * M, -0.6 * M, 2.0 * M, 0.6 * M), street)
	for d in [[SHAFT_END + 5.0, 2.4, 0.35], [SHAFT_END + 10.5, 3.5, 0.5], [SHAFT_END + 17.0, 2.8, 0.3]]:
		var pts := PackedVector2Array()
		for k in range(13):
			var a := PI * k / 12.0
			pts.append(Vector2((d[0] - cos(a) * d[1] / 2.0) * M, -sin(a) * d[2] * M))
		draw_colored_polygon(pts, Color(0.1, 0.11, 0.13))
