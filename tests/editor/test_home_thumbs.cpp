// The Home window's project preview images: where each project's thumbnail is stored.
#include "../Test.h"
#include "Util.h"
#include <string>

using namespace osteon;

TEST(project_thumb_path_is_stable_and_in_the_thumbs_folder) {
    std::string a = projectThumbPath("C:/Games/Hero/hero.ost", "D:/data");
    CHECK(a == projectThumbPath("C:/Games/Hero/hero.ost", "D:/data"));        // the same project always maps to the same file
    CHECK(a.rfind("D:/data/thumbs/", 0) == 0);                                // kept in the app data folder
    CHECK(a.size() > 4 && a.substr(a.size() - 4) == ".png");
}

TEST(project_thumb_path_ignores_case_and_separators_but_not_the_project) {
    std::string a = projectThumbPath("C:/Games/Hero/hero.ost", "D:/data");
    CHECK(a == projectThumbPath("c:\\games\\HERO\\Hero.OST", "D:/data"));     // Windows paths: case and slashes do not matter
    CHECK(a != projectThumbPath("C:/Games/Hero/villain.ost", "D:/data"));    // another project gets its own preview
    CHECK(a != projectThumbPath("C:/Games/Other/hero.ost", "D:/data"));      // so does the same name in another folder
}
