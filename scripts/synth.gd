class_name Synth
extends RefCounted
## Звуки, собранные кодом (без файлов). Всё моно, 22050 Гц, 16 бит.

const RATE := 22050


static func _to_wav(samples: PackedFloat32Array, loop := false) -> AudioStreamWAV:
	var data := PackedByteArray()
	data.resize(samples.size() * 2)
	for i in range(samples.size()):
		var v := int(clampf(samples[i], -1.0, 1.0) * 32767.0)
		data.encode_s16(i * 2, v)
	var w := AudioStreamWAV.new()
	w.format = AudioStreamWAV.FORMAT_16_BITS
	w.mix_rate = RATE
	w.stereo = false
	w.data = data
	if loop:
		w.loop_mode = AudioStreamWAV.LOOP_FORWARD
		w.loop_begin = 0
		w.loop_end = samples.size()
	return w


## Низкий гул труб и вентиляции (петля 4 с).
static func hum() -> AudioStreamWAV:
	var n := RATE * 4
	var s := PackedFloat32Array()
	s.resize(n)
	var rng := RandomNumberGenerator.new()
	rng.seed = 7
	var lp := 0.0
	for i in range(n):
		var t := float(i) / RATE
		# частоты кратны 0,25 Гц, чтобы петля сходилась без щелчка
		var v := 0.30 * sin(TAU * 50.0 * t) + 0.12 * sin(TAU * 100.0 * t) + 0.05 * sin(TAU * 150.25 * t)
		v *= 0.75 + 0.25 * sin(TAU * 0.25 * t)
		lp += (rng.randf_range(-1.0, 1.0) - lp) * 0.02
		s[i] = v * 0.6 + lp * 0.5
	return _to_wav(s, true)


## Дыхание в маске: вдох-выдох, глухо и с клапаном. period — длина цикла, с.
static func breath(period := 3.2, heavy := false) -> AudioStreamWAV:
	var n := int(RATE * period)
	var s := PackedFloat32Array()
	s.resize(n)
	var rng := RandomNumberGenerator.new()
	rng.seed = 11
	var lp := 0.0
	var lp2 := 0.0
	var cut := 0.08 if heavy else 0.05
	for i in range(n):
		var ph := float(i) / n
		var env := 0.0
		if ph < 0.38:
			env = sin(PI * ph / 0.38)          # вдох
		elif ph > 0.45 and ph < 0.85:
			env = 0.8 * sin(PI * (ph - 0.45) / 0.40)  # выдох
		env = pow(env, 1.5)
		var noise := rng.randf_range(-1.0, 1.0)
		lp += (noise - lp) * cut
		lp2 += (lp - lp2) * cut
		var valve := 0.0
		if ph > 0.45 and ph < 0.47:
			valve = 0.25 * sin(TAU * 90.0 * float(i) / RATE)   # щелчок клапана
		s[i] = (lp2 * 3.2 * env + valve) * (1.0 if heavy else 0.8)
	return _to_wav(s, true)


## Короткий шаг по металлу/бетону.
static func step() -> AudioStreamWAV:
	var n := int(RATE * 0.12)
	var s := PackedFloat32Array()
	s.resize(n)
	var rng := RandomNumberGenerator.new()
	rng.seed = 3
	var lp := 0.0
	for i in range(n):
		var t := float(i) / RATE
		var env := exp(-t * 45.0)
		lp += (rng.randf_range(-1.0, 1.0) - lp) * 0.25
		s[i] = (lp * 0.9 + 0.3 * sin(TAU * 110.0 * t)) * env
	return _to_wav(s)


## Сухой треск корней (рассыпаются в пыль).
static func crackle(seconds := 1.4) -> AudioStreamWAV:
	var n := int(RATE * seconds)
	var s := PackedFloat32Array()
	s.resize(n)
	var rng := RandomNumberGenerator.new()
	rng.seed = 5
	var lp := 0.0
	for i in range(n):
		var t := float(i) / RATE
		var env := clampf(1.0 - t / seconds, 0.0, 1.0)
		var v := 0.0
		if rng.randf() < 0.012 * env:
			v = rng.randf_range(-1.0, 1.0)
		lp += (rng.randf_range(-1.0, 1.0) - lp) * 0.04
		s[i] = v * 0.9 + lp * 0.6 * env
	return _to_wav(s)


## Импульс Дефлотатора: гудящий нарастающий тон с треском проводки.
static func beam(seconds := 1.0) -> AudioStreamWAV:
	var n := int(RATE * seconds)
	var s := PackedFloat32Array()
	s.resize(n)
	var rng := RandomNumberGenerator.new()
	rng.seed = 9
	var ph := 0.0
	for i in range(n):
		var t := float(i) / RATE
		var f := 180.0 + 260.0 * (t / seconds)
		ph += TAU * f / RATE
		var env := minf(1.0, t * 20.0) * clampf((seconds - t) * 6.0, 0.0, 1.0)
		var buzz := signf(sin(ph)) * 0.25 + sin(ph * 2.01) * 0.2
		var crack := rng.randf_range(-1.0, 1.0) * (0.35 if rng.randf() < 0.05 else 0.05)
		s[i] = (buzz + crack) * env * 0.7
	return _to_wav(s)


## Шипение (туман врывается, перегрев колбы).
static func hiss(seconds := 2.0) -> AudioStreamWAV:
	var n := int(RATE * seconds)
	var s := PackedFloat32Array()
	s.resize(n)
	var rng := RandomNumberGenerator.new()
	rng.seed = 13
	var lp := 0.0
	for i in range(n):
		var t := float(i) / RATE
		var env := minf(1.0, t * 8.0) * clampf((seconds - t) / seconds * 1.5, 0.0, 1.0)
		var v := rng.randf_range(-1.0, 1.0)
		lp += (v - lp) * 0.5
		s[i] = (v - lp) * env * 0.5
	return _to_wav(s)


## Кашель.
static func cough() -> AudioStreamWAV:
	var n := int(RATE * 0.7)
	var s := PackedFloat32Array()
	s.resize(n)
	var rng := RandomNumberGenerator.new()
	rng.seed = 17
	var lp := 0.0
	for i in range(n):
		var t := float(i) / RATE
		var env := exp(-fmod(t, 0.33) * 12.0) * (1.0 if t < 0.66 else 0.0)
		lp += (rng.randf_range(-1.0, 1.0) - lp) * 0.12
		s[i] = (lp * 2.5 + 0.2 * sin(TAU * 140.0 * t)) * env
	return _to_wav(s)


## Мягкий светлый «текстурный» круг для 2D-света.
static func light_texture(size := 256) -> GradientTexture2D:
	var g := Gradient.new()
	g.set_color(0, Color(1, 1, 1, 1))
	g.set_color(1, Color(1, 1, 1, 0))
	g.add_point(0.35, Color(1, 1, 1, 0.55))
	var t := GradientTexture2D.new()
	t.gradient = g
	t.fill = GradientTexture2D.FILL_RADIAL
	t.fill_from = Vector2(0.5, 0.5)
	t.fill_to = Vector2(1.0, 0.5)
	t.width = size
	t.height = size
	return t
