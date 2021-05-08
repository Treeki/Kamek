#include <kamek.h>

/*
 * A tiny patch to make the Nvidia Shield port of NSMBW work on Dolphin.
 *
 * This may or may not work on a Wii. The sloppy edits Nvidia/Lightspeed did
 * to daLiftRemoconSeesaw_c (the player-tiltable platform seen in World 1-2)
 * cause the game to read from 0x00000074 when nobody is riding the platform
 * as it tries to check the device type of a null pointer.
 *
 * Amazing.
 */

namespace EGG {
	struct Vector3f {
		float x, y, z;
		void normalise();
	};
}

struct MTX34 { float m[3][4]; };

struct DRMStruct {
	u32 in_1;
	f32 in_screenHeight;
	f32 in_screenWidth;
	EGG::Vector3f in_camPos;
	EGG::Vector3f in_camTarget;
	EGG::Vector3f in_camUp;
	EGG::Vector3f out_camPos0;
	EGG::Vector3f out_camTarget0;
	EGG::Vector3f out_camUp0;
	EGG::Vector3f out_camPos1;
	EGG::Vector3f out_camTarget1;
	EGG::Vector3f out_camUp1;
	f32 out_orthoTop;
	f32 out_orthoBottom;
	f32 out_orthoLeft;
	f32 out_orthoRight;
	MTX34 out_camMtx0;
	MTX34 out_camMtx1;
};

void calculateCamera(const EGG::Vector3f &pos, const EGG::Vector3f &target, const EGG::Vector3f &up, MTX34 &mtx) {
	EGG::Vector3f a = { pos.x - target.x, pos.y - target.y, pos.z - target.z };
	a.normalise();
	EGG::Vector3f b = { up.y * a.z - up.z * a.y, up.z * a.x - up.x * a.z, up.x * a.y - up.y * a.x };
	b.normalise();
	EGG::Vector3f c = { a.y * b.z - a.z * b.y, a.z * b.x - a.x * b.z, a.x * b.y - a.y * b.x };
	c.normalise();

	mtx.m[0][0] = b.x;
	mtx.m[0][1] = b.y;
	mtx.m[0][2] = b.z;
	mtx.m[0][3] = -(b.z * pos.z + b.x * pos.x + b.y * pos.y);
	mtx.m[1][0] = c.x;
	mtx.m[1][1] = c.y;
	mtx.m[1][2] = c.z;
	mtx.m[1][3] = -(c.z * pos.z + c.x * pos.x + c.y * pos.y);
	mtx.m[2][0] = a.x;
	mtx.m[2][1] = a.y;
	mtx.m[2][2] = a.z;
	mtx.m[2][3] = -(a.z * pos.z + a.x * pos.x + a.y * pos.y);
}

// lingcod_callNVSISnippet
kmBranchDefCpp(0x80100840, NULL, void, DRMStruct *data) {
	// This logic is stripped out of the game and executed by
	// the "NVSI" DRM logic in the Lingcod emulator.
	// So, we reimplement it here
	data->out_camPos0 = data->in_camPos;
	data->out_camTarget0 = data->in_camTarget;
	data->out_camUp0 = data->in_camUp;
	calculateCamera(data->out_camPos0, data->out_camTarget0, data->out_camUp0, data->out_camMtx0);

	f32 y = 0.5f * data->in_screenHeight;
	data->out_orthoTop = y;
	data->out_orthoBottom = -y;
	f32 x = 0.5f * -data->in_screenWidth;
	data->out_orthoLeft = x;
	data->out_orthoRight = -x;

	data->out_camPos1 = (EGG::Vector3f){ 0.0f, 0.0f, 0.0f };
	data->out_camTarget1 = (EGG::Vector3f){ 0.0f, 0.0f, 0.0f - 100.0f };
	data->out_camUp1 = data->in_camUp;
	calculateCamera(data->out_camPos1, data->out_camTarget1, data->out_camUp1, data->out_camMtx1);
}

// lingcod_getIniState
kmBranchDefCpp(0x801007B0, NULL, bool, char *section, char *key) {
	if (strcmp(section, "NSMB") == 0) {
		// reference: e_assets/ini/nsmb/nsmb.ini
		if (strcmp(key, "AUTO_PILOTING") == 0) return false;
		if (strcmp(key, "CREDIT_SCREEN_SHORTCUT") == 0) return false;
	}

	if (strcmp(section, "SPLASH_SCREENS") == 0) {
		// reference: e_assets/ini/debug.ini
		if (strcmp(key, "SKIP_STRAP_SCREEN") == 0) return true;
		if (strcmp(key, "SKIP_CONTROLLER_INFO") == 0) return true;
	}

	return false;
}

// lingcod_getIniKeyValueInt16
kmBranchDefCpp(0x801007D0, NULL, short, char *section, char *key, short defaultValue) {
	if (strcmp(section, "NSMB") == 0) {
		// reference: e_assets/ini/nsmb/nsmb.ini
		if (strcmp(key, "NOTCH_SPEED") == 0) return 0x1C0;
		if (strcmp(key, "CANNON_SPEED") == 0) return 0x140;
		if (strcmp(key, "WIRE_ADD_RATE") == 0) return 0x500;
		if (strcmp(key, "WIRE_SUB_RATE") == 0) return 0x500;

		// can be set to a value from 0 to 68 to run a specific course
		// else, runs through all courses
		if (strcmp(key, "AUTO_PILOTING_COURSE_INDEX") == 0) return -1;
	}

	return defaultValue;
}

// lingcod_getIniKeyValueFloat
kmBranchDefCpp(0x801007E0, NULL, float, char *section, char *key, float defaultValue) {
	if (strcmp(section, "NSMB") == 0) {
		// reference: e_assets/ini/nsmb/nsmb.ini
		if (strcmp(key, "NINTENDO_LOGO_TIME") == 0) return 2.0f;
		if (strcmp(key, "NINTENDO_LOGO_CANCEL_TIME") == 0) return 1.0f;
		if (strcmp(key, "NV_LOGO_TIME") == 0) return 2.0f;
		if (strcmp(key, "NV_LOGO_CANCEL_TIME") == 0) return 0.0f;
		if (strcmp(key, "MOC_SCREEN_TIME") == 0) return 2.0f;
		if (strcmp(key, "MOC_SCREEN_CANCEL_TIME") == 0) return 0.0f;

		// not used...?
		if (strcmp(key, "DOF_SCALE") == 0) return 1.5f;
	}

	return defaultValue;
}

