-- MySQL Workbench Forward Engineering

SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0;
SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0;
SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION';

-- -----------------------------------------------------
-- Schema mydb
-- -----------------------------------------------------
-- -----------------------------------------------------
-- Schema tmgreport
-- -----------------------------------------------------

-- -----------------------------------------------------
-- Schema tmgreport
-- -----------------------------------------------------
CREATE SCHEMA IF NOT EXISTS `tmgreport` DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci ;
USE `tmgreport` ;

-- -----------------------------------------------------
-- Table `tmgreport`.`clientfilter`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `tmgreport`.`clientfilter` (
  `idclientFilter` INT(11) NOT NULL AUTO_INCREMENT,
  `nameClientFilter` TEXT NULL DEFAULT NULL,
  PRIMARY KEY (`idclientFilter`))
ENGINE = InnoDB
AUTO_INCREMENT = 7
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_0900_ai_ci;


-- -----------------------------------------------------
-- Table `tmgreport`.`day_logs`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `tmgreport`.`day_logs` (
  `idlogs` INT(11) NOT NULL AUTO_INCREMENT,
  `host_ip` TEXT NULL DEFAULT NULL,
  `client_user_name` TINYTEXT NULL DEFAULT NULL,
  `target_host` TEXT NULL DEFAULT NULL,
  `target_path` TEXT NULL DEFAULT NULL,
  `Date` DATETIME NULL DEFAULT NULL,
  `upload` DECIMAL(20,0) NULL DEFAULT NULL,
  `download` DECIMAL(20,0) NULL DEFAULT NULL,
  PRIMARY KEY (`idlogs`))
ENGINE = InnoDB
AUTO_INCREMENT = 10384
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_0900_ai_ci;


-- -----------------------------------------------------
-- Table `tmgreport`.`day_result`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `tmgreport`.`day_result` (
  `id` INT(11) NOT NULL AUTO_INCREMENT,
  `day` DATE NULL DEFAULT NULL,
  `user` TEXT NULL DEFAULT NULL,
  `upload` DECIMAL(20,0) NULL DEFAULT NULL,
  `download` DECIMAL(20,0) NULL DEFAULT NULL,
  PRIMARY KEY (`id`))
ENGINE = InnoDB
AUTO_INCREMENT = 1361
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_0900_ai_ci;


-- -----------------------------------------------------
-- Table `tmgreport`.`filter`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `tmgreport`.`filter` (
  `idfilter` INT(11) NOT NULL AUTO_INCREMENT,
  `userfilter` TEXT NULL DEFAULT NULL,
  `targetfilter` TEXT NULL DEFAULT NULL,
  PRIMARY KEY (`idfilter`))
ENGINE = InnoDB
AUTO_INCREMENT = 18
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_0900_ai_ci;


-- -----------------------------------------------------
-- Table `tmgreport`.`logs`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `tmgreport`.`logs` (
  `idlogs` INT(11) NOT NULL AUTO_INCREMENT,
  `client_user_name` TINYTEXT NULL DEFAULT NULL,
  `target_host` TEXT NULL DEFAULT NULL,
  `Date` DATE NULL DEFAULT NULL,
  `upload` DECIMAL(20,0) NULL DEFAULT NULL,
  `download` DECIMAL(20,0) NULL DEFAULT NULL,
  PRIMARY KEY (`idlogs`))
ENGINE = InnoDB
AUTO_INCREMENT = 95069
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_0900_ai_ci;


-- -----------------------------------------------------
-- Table `tmgreport`.`targetfilter`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `tmgreport`.`targetfilter` (
  `idtargetFilter` INT(11) NOT NULL AUTO_INCREMENT,
  `nameFilter` TEXT NULL DEFAULT NULL,
  PRIMARY KEY (`idtargetFilter`))
ENGINE = InnoDB
AUTO_INCREMENT = 4
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_0900_ai_ci;


-- -----------------------------------------------------
-- Table `tmgreport`.`users`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `tmgreport`.`users` (
  `idusers` INT(11) NOT NULL AUTO_INCREMENT,
  `name` VARCHAR(80) NULL DEFAULT NULL,
  `status` VARCHAR(45) NULL DEFAULT 'user',
  PRIMARY KEY (`idusers`))
ENGINE = InnoDB
AUTO_INCREMENT = 7
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_0900_ai_ci;


SET SQL_MODE=@OLD_SQL_MODE;
SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS;
SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS;
