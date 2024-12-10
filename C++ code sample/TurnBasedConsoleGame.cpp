#include <iostream>
#include <vector>
#include <string>
#include <cstdlib>
#include <ctime>
#include <sstream> 

class Team;
class Monster {
public:
    std::string name;
    int health;
    int damage;
    int block;
    int reflect;
    std::string color;
    Monster(std::string n, int h, int d, int b, int r) : name(n), health(h), damage(d), block(b), reflect(r) {}

    virtual void attack(Monster& target) {
        int actualDamage = damage - target.block;
        if (actualDamage < 0) actualDamage = 0;
        target.health -= actualDamage;
        health -= target.reflect;
        std::cout <<color<<" "<< name << " attacks " << target.color << " " << target.name << " for " << damage << " damage"
            << (target.block > 0 ? ", dealing " + std::to_string(actualDamage) + " damage" : "")
            << (target.reflect > 0 ? ", and receives " + std::to_string(target.reflect) + " reflected damage" : "")
            << "\n";
    }

    virtual void endTurn() {}
    bool isAlive() const { return health > 0; }
};

class Goblin : public Monster {
public:
    int numAttacks;

    Goblin(std::string n="Goblin", int h = 36, int d = 15, int na = 4, int b = 0, int r = 0) : Monster(n, h, d, b, r), numAttacks(na) {}

    void attack(Monster& target) override {
        for (int i = 0; i < numAttacks; ++i) {
            Monster::attack(target);
        }
    }
};

class Troll : public Monster {
public:
    int regen;

    Troll(std::string n="Troll", int h = 100, int d = 20, int b=0, int ref = 0, int reg = 10) : Monster(n, h, d, b, ref), regen(reg) {}

    void endTurn() override {
        health += regen;
        std::cout << name << " regenerates " << regen << " health\n";
    }
};

class Orc : public Monster {
public:

    Orc(std::string n = "Orc", int h = 80, int d = 15, int b = 5, int r = 5) : Monster(n, h, d, b, r) {}
};

class Team {
public:
    std::string name;
    std::vector<Monster*> monsters;

    Team(std::string n) : name(n) {}

    void addMonster(Monster* m) {
        m->color = name;
        monsters.push_back(m);
    }

    bool isAlive() const {
        for (auto m : monsters) {
            if (m->isAlive()) return true;
        }
        return false;
    }

    Monster* getLeadMonster() {
        for (auto m : monsters) {
            if (m->isAlive()) return m;
        }
        return nullptr;
    }

    std::string GetStatus() const {
        std::ostringstream oss;
        oss << "[ " << name << " | ";
        for (int i = monsters.size() - 1; i >= 0; --i) {
            auto m = monsters[i];
            if (m->isAlive()) {
                oss << m->name << "(" << m->health << ") ";
            }
        }
        oss << "]";
        return oss.str();
    }
    std::string GetReversedStatus() const {
        std::ostringstream oss;
        oss << "[ ";        
        for (auto m : monsters) {
            if (m->isAlive()) {
                oss << m->name << "(" << m->health << ") ";
            }
        }
        oss << " | " << name  << "]";
        return oss.str();
    }
};

void battle(Team& team1, Team& team2) {
    std::cout << "Battle Starting\n";
    std::cout << team1.GetStatus() << "..." << team2.GetReversedStatus() << "\n";
    int turn = 1;
    while (team1.isAlive() && team2.isAlive()) {
        std::cout << "-----------------------------------------------------------------------------------------------------------------------\n";
        std::cout << "Turn " << turn << "\n";

        std::cout << team1.GetStatus() << "..." << team2.GetReversedStatus() << "\n";

        Monster* m1 = team1.getLeadMonster();
        Monster* m2 = team2.getLeadMonster();

        if (m1 && m2) {
            m1->attack(*m2);
            if (m2->isAlive()) {
                m2->attack(*m1);
            }
            if (m1->isAlive()) {
                m1->endTurn();
            }
            else
                {
				std::cout << m1->color << " " << m1->name << " has dead\n";
			}
            if (m2->isAlive()) {
                m2->endTurn();
            }
            else
            {
                std::cout << m2->color << " " << m2->name << " has dead\n";
            }
        }

        turn++;
    }

    std::cout << "Battle over. ";
    if (team1.isAlive()) {
        std::cout << team1.name << " team wins!\n";
    }
    else {
        std::cout << team2.name << " team wins!\n";
    }
    std::cout << team1.GetStatus() <<"..." << team2.GetReversedStatus() << "\n";
    std::cout << "=======================================================================================================================\n";
}

int main() {
    srand(static_cast<unsigned int>(time(0)));

    // Battle 1: One goblin vs one troll
    Team red1("Red");
    red1.addMonster(new Goblin());
    Team blue1("Blue");
    blue1.addMonster(new Troll());
    battle(red1, blue1);

    // Battle 2: One goblin vs two trolls
    Team red2("Red");
    red2.addMonster(new Goblin());
    Team blue2("Blue");
    blue2.addMonster(new Troll());
    blue2.addMonster(new Troll());
    battle(red2, blue2);

    // Battle 3: One troll vs one orc
    Team red3("Red");
    red3.addMonster(new Troll());
    Team blue3("Blue");
    blue3.addMonster(new Orc());
    battle(red3, blue3);

    // Battle 4: One troll vs two orcs
    Team red4("Red");
    red4.addMonster(new Troll());
    Team blue4("Blue");
    blue4.addMonster(new Orc());
    blue4.addMonster(new Orc());
    battle(red4, blue4);

    // Battle 5: One orc vs one goblin
    Team red5("Red");
    red5.addMonster(new Orc());
    Team blue5("Blue");
    blue5.addMonster(new Goblin());
    battle(red5, blue5);

    // Battle 6: One orc vs two goblins
    Team red6("Red");
    red6.addMonster(new Orc());
    Team blue6("Blue");
    blue6.addMonster(new Goblin());
    blue6.addMonster(new Goblin());
    battle(red6, blue6);

    // Battle 7: 4 random monsters vs 4 random monsters
    Team red7("Red");
    Team blue7("Blue");
    for (int i = 0; i < 4; ++i) {
        int type = rand() % 3;
        if (type == 0) {
            red7.addMonster(new Goblin());
        }
        else if (type == 1) {
            red7.addMonster(new Troll());
        }
        else {
            red7.addMonster(new Orc());
        }
    }
    for (int i = 0; i < 4; ++i) {
        int type = rand() % 3;
        if (type == 0) {
            blue7.addMonster(new Goblin());
        }
        else if (type == 1) {
            blue7.addMonster(new Troll());
        }
        else {
            blue7.addMonster(new Orc());
        }
    }
    battle(red7, blue7);

    return 0;
}