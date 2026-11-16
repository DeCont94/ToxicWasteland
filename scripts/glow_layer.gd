extends Node2D
## Слой того, что светится само: его не затемняет полярная ночь.
## layer = "back" — небо и город за шахтой (с параллаксом), "front" — корни, бутоны, луч.

var level: Node2D
var layer := "front"
const M := Balance.PX_PER_M


func _draw() -> void:
	if level == null:
		return
	if layer == "back":
		_draw_city()
	else:
		_draw_front()


func _draw_front() -> void:
	var t: float = level.now
	# бутоны и мох на теле деда — тускло, в ритме дыхания Артёма
	var b: float = level.bud_light.energy
	var gx: float = level.GRANDPA_X * M
	var buds := [Vector2(0.1, -1.05), Vector2(0.28, -0.98), Vector2(-0.12, -0.8), Vector2(0.3, -1.38), Vector2(0.62, -0.97), Vector2(0.02, -0.6)]
	for i in range(buds.size()):
		var p: Vector2 = buds[i]
		draw_circle(Vector2(gx + p.x * M, p.y * M), 3.0 + (i % 3), Color(0.35, 1.0, 0.85, 0.35 + 0.5 * b))
	# стена корней: красные, шипастые, пульсируют
	if level.roots_dust < 1.2:
		var fade: float = 1.0 - clampf(level.roots_dust / 1.2, 0.0, 1.0)
		var a: float = level.ROOTS_A * M
		var w: float = (level.ROOTS_B - level.ROOTS_A) * M
		var pulse := 0.75 + 0.25 * sin(t * 3.0)
		var rng := RandomNumberGenerator.new()
		rng.seed = 42
		for i in range(26):
			var x0: float = a + rng.randf() * w
			var from_top := rng.randf() < 0.5
			var y0: float = -level.CEIL * M if from_top else 0.0
			var pts := PackedVector2Array()
			var x: float = x0
			var y: float = y0
			var segs := 9
			for k in range(segs + 1):
				pts.append(Vector2(x, y))
				x += rng.randf_range(-0.18, 0.18) * M + sin(t * 1.5 + i + k) * 1.5
				y += (level.CEIL * M / segs) * (1.0 if from_top else -1.0) * rng.randf_range(0.6, 1.1)
			var col := Color(0.75, 0.08, 0.06, fade).lerp(Color(1.0, 0.3, 0.2, fade), pulse * rng.randf())
			draw_polyline(pts, col, rng.randf_range(3.0, 7.0))
			# шипы
			for k in range(2, pts.size() - 1, 2):
				var q := pts[k]
				draw_line(q, q + Vector2(rng.randf_range(-10, 10), rng.randf_range(-10, 10)), col, 2.0)
	# луч Дефлотатора
	if level.beam_t > 0.0:
		var from: Vector2 = level._hand()
		var to: Vector2 = from + level.beam_dir * 8.0 * M
		var fl := 0.6 + 0.4 * sin(t * 60.0)
		draw_line(from, to, Color(0.6, 1.0, 0.95, 0.25 * fl), 18.0)
		draw_line(from, to, Color(0.85, 1.0, 1.0, 0.85 * fl), 3.0)
	# колба Дефлотатора в руке: светится, краснеет от нагрева
	if level.has_deflotator():
		var hp: Vector2 = level._hand()
		var h: float = level.heat / 100.0
		var c := Color(0.5, 0.95, 0.9).lerp(Color(1.0, 0.35, 0.2), h)
		draw_circle(hp, 4.0 + 2.0 * h, Color(c.r, c.g, c.b, 0.85))
		draw_circle(hp, 10.0 + 4.0 * h, Color(c.r, c.g, c.b, 0.15))
	# отладка
	if level.debug:
		var f := ThemeDB.fallback_font
		draw_string(f, level.player.position + Vector2(-80, -2.4 * M), "нагрев %d · тревога %d · фильтр %d" % [int(level.heat), level.alarm, int(level.player.filter_left)], HORIZONTAL_ALIGNMENT_CENTER, 160, 13, Color(0.8, 0.9, 0.85))


func _draw_city() -> void:
	var cam_x: float = level.camera.get_screen_center_position().x
	var start: float = level.SHAFT_END * M
	# небо полярной ночи
	var sky_top := Color(0.01, 0.015, 0.03)
	var sky_low := Color(0.05, 0.13, 0.12)
	var x0 := start - 2 * M
	var x1: float = (level.LEVEL_END + 14.0) * M
	for i in range(16):
		var k := float(i) / 16.0
		draw_rect(Rect2(x0, -14.0 * M + k * 14.0 * M, x1 - x0, 14.0 * M / 16.0 + 1), sky_top.lerp(sky_low, k))
	# дальний план: тундра и три башни «Альфы» (медленный параллакс)
	var far := _par(cam_x, 0.12)
	draw_rect(Rect2(x0, -1.6 * M, x1 - x0, 1.6 * M), Color(0.02, 0.03, 0.04))
	var tx: float = (level.SHAFT_END + 7.0) * M + far
	for k in range(3):
		var bx: float = tx + k * 1.3 * M
		var hgt: float = (7.0 + k * 0.8) * M
		draw_colored_polygon(PackedVector2Array([Vector2(bx - 0.35 * M, -1.6 * M), Vector2(bx + 0.35 * M, -1.6 * M),
			Vector2(bx + 0.18 * M, -hgt), Vector2(bx - 0.18 * M, -hgt)]), Color(0.015, 0.02, 0.03))
		draw_circle(Vector2(bx, -hgt), 3.0, Color(1.0, 0.7, 0.3, 0.6 + 0.3 * sin(level.now * 1.3 + k)))
	draw_rect(Rect2(tx - 1.2 * M, -3.0 * M, 5.0 * M, 1.4 * M), Color(0.015, 0.02, 0.03))
	# средний план: панельки на сваях, перекошенные, пронзённые светящимися деревьями
	var mid := _par(cam_x, 0.45)
	var houses := [[3.0, 6.0, 5.5, -0.07], [9.5, 5.0, 7.0, 0.05], [15.5, 6.5, 6.0, -0.1], [22.0, 5.5, 8.0, 0.04]]
	for hd in houses:
		var hx: float = start + hd[0] * M + mid
		var hw: float = hd[1] * M
		var hh: float = hd[2] * M
		var tilt: float = hd[3]
		var base := -1.2 * M
		var poly := PackedVector2Array([Vector2(hx, base), Vector2(hx + hw, base + tilt * hw),
			Vector2(hx + hw + tilt * hh, base - hh + tilt * hw), Vector2(hx + tilt * hh, base - hh)])
		draw_colored_polygon(poly, Color(0.012, 0.016, 0.02))
		for s in range(5):
			var sx := hx + (0.1 + s * 0.2) * hw
			draw_line(Vector2(sx, base + tilt * (sx - hx)), Vector2(sx, 0), Color(0.03, 0.035, 0.045), 5)
		# окна: тёмные, кроме редких отблесков
		for wy in range(int(hh / M) - 1):
			for wx in range(int(hw / M) - 1):
				if (wx * 7 + wy * 3 + int(hd[0])) % 5 == 0:
					var wp := Vector2(hx + (wx + 0.7) * M + tilt * wy * M, base - (wy + 1.0) * M + tilt * (wx + 0.7) * M)
					draw_rect(Rect2(wp, Vector2(0.25 * M, 0.35 * M)), Color(0.2, 0.6, 0.5, 0.25))
		# дерево-паразит: ствол сквозь дом, светящаяся крона
		var trunk := Vector2(hx + hw * 0.55, 0)
		var top := trunk + Vector2(tilt * 30.0, -(hh + 1.8 * M))
		draw_line(trunk, top, Color(0.05, 0.12, 0.1), 7)
		var tints := [Color(0.2, 0.95, 0.8), Color(0.25, 0.9, 0.45), Color(1.0, 0.72, 0.3)]
		var col: Color = tints[int(hd[0]) % 3]
		for c in range(7):
			var off := Vector2(sin(c * 2.1) * 1.1 * M, cos(c * 1.7) * 0.6 * M - 0.3 * M)
			var r := (0.45 + 0.15 * sin(level.now * 0.8 + c)) * M
			draw_circle(top + off, r * 2.2, Color(col.r, col.g, col.b, 0.09))
			draw_circle(top + off, r * 0.6, Color(col.r, col.g, col.b, 0.35))
	# туманная полоса у земли
	for f in range(4):
		var fy := -0.4 * M - f * 0.35 * M
		draw_rect(Rect2(x0, fy, x1 - x0, 0.35 * M), Color(0.35, 0.5, 0.45, 0.05))


func _par(cam_x: float, factor: float) -> float:
	# чем меньше factor, тем медленнее слой движется относительно камеры
	return (cam_x - level.SHAFT_END * M) * (1.0 - factor)
