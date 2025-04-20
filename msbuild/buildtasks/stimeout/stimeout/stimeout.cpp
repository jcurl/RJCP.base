#include <iostream>
#include <chrono>
#include <thread>
#include <string>

auto main(int argc, const char* argv[]) -> int {
  if (argc != 2) {
    std::cerr << "Usage: " << argv[0] << " time" << std::endl;
    return 1;
  }

  int time;
  try {
    time = std::stoi(argv[1]);
  } catch (std::invalid_argument&) {
    std::cerr << "Invalid time argument" << std::endl;
    return 1;
  } catch (std::out_of_range&) {
    std::cerr << "out of range time argument" << std::endl;
    return 1;
  }

  std::cout << "Sleeping for " << time << " seconds" << std::endl;
  std::this_thread::sleep_for(std::chrono::seconds(time));
  return 0;
}
